using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.HardwareInfo
{
    public static class HardwareInfoReader
    {
        public static HardwareInfoSnapshot GetCurrent(ILogger? logger = null)
        {
            var snapshot = new HardwareInfoSnapshot();

            ReadCpuInfo(snapshot, logger);
            ReadDiskInfo(snapshot, logger);
            ReadMacInfo(snapshot, logger);
            ReadMotherboardInfo(snapshot, logger);

            return snapshot;
        }

        private static void ReadCpuInfo(HardwareInfoSnapshot snapshot, ILogger? logger)
        {
            // 优先用 GetSystemInfo 取核心数，以便注入 Hook 后能显示配置中的模拟值
            try
            {
                GetSystemInfoNative(out var sysInfo);
                if (sysInfo.dwNumberOfProcessors > 0)
                {
                    snapshot.CpuCoreCount = (int)sysInfo.dwNumberOfProcessors;
                }
            }
            catch (Exception ex)
            {
                logger?.Error("GetSystemInfo 取核心数失败，将使用 WMI。", ex, "HardwareInfo");
            }

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get().Cast<ManagementObject>())
                    {
                        snapshot.CpuModel = obj["Name"]?.ToString() ?? snapshot.CpuModel;

                        if (snapshot.CpuCoreCount <= 0 && int.TryParse(obj["NumberOfCores"]?.ToString(), out var cores) && cores > 0)
                        {
                            snapshot.CpuCoreCount = cores;
                        }

                        snapshot.CpuId = obj["ProcessorId"]?.ToString() ?? snapshot.CpuId;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error("读取 CPU 信息失败。", ex, "HardwareInfo");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetSystemInfoNative(out SYSTEM_INFO lpSystemInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        private static void ReadDiskInfo(HardwareInfoSnapshot snapshot, ILogger? logger)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia"))
                {
                    foreach (var obj in searcher.Get().Cast<ManagementObject>())
                    {
                        var serial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(serial))
                        {
                            snapshot.DiskSerial = serial.Trim();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error("读取硬盘序列号失败。", ex, "HardwareInfo");
            }
        }

        private static void ReadMacInfo(HardwareInfoSnapshot snapshot, ILogger? logger)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                           "SELECT MACAddress, IPEnabled FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE"))
                {
                    foreach (var obj in searcher.Get().Cast<ManagementObject>())
                    {
                        var mac = obj["MACAddress"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(mac))
                        {
                            snapshot.MacAddress = mac;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error("读取 MAC 地址失败。", ex, "HardwareInfo");
            }
        }

        private static void ReadMotherboardInfo(HardwareInfoSnapshot snapshot, ILogger? logger)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get().Cast<ManagementObject>())
                    {
                        var serial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(serial))
                        {
                            snapshot.MotherboardSerial = serial.Trim();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error("读取主板序列号失败。", ex, "HardwareInfo");
            }
        }
    }
}

