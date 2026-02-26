using System;
using System.Management;
using System.Text;

namespace HardwareHook.Core.HardwareInfo
{
    /// <summary>
    /// 硬件信息读取器
    /// </summary>
    public class HardwareInfoReader
    {
        /// <summary>
        /// 读取CPU信息
        /// </summary>
        /// <returns>CPU信息快照</returns>
        public static HardwareInfoSnapshot ReadHardwareInfo()
        {
            var snapshot = new HardwareInfoSnapshot();

            try
            {
                // 读取CPU信息
                ReadCpuInfo(snapshot);

                // 读取硬盘信息
                ReadDiskInfo(snapshot);

                // 读取MAC地址
                ReadMacAddress(snapshot);

                // 读取主板信息
                ReadMotherboardInfo(snapshot);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                snapshot.Error = ex.Message;
            }

            return snapshot;
        }

        /// <summary>
        /// 读取CPU信息
        /// </summary>
        /// <param name="snapshot">硬件信息快照</param>
        private static void ReadCpuInfo(HardwareInfoSnapshot snapshot)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        snapshot.CpuModel = item["Name"]?.ToString() ?? "Unknown";
                        snapshot.CpuCoreCount = int.Parse(item["NumberOfCores"]?.ToString() ?? "0");
                        snapshot.CpuId = GetCpuId();
                        break;
                    }
                }
            }
            catch
            {
                snapshot.CpuModel = "Unknown";
                snapshot.CpuCoreCount = 0;
                snapshot.CpuId = "Unknown";
            }
        }

        /// <summary>
        /// 读取硬盘信息
        /// </summary>
        /// <param name="snapshot">硬件信息快照</param>
        private static void ReadDiskInfo(HardwareInfoSnapshot snapshot)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType = 'IDE' OR InterfaceType = 'SATA'"))
                {
                    foreach (var item in searcher.Get())
                    {
                        snapshot.DiskSerial = item["SerialNumber"]?.ToString()?.Trim() ?? "Unknown";
                        break;
                    }
                }
            }
            catch
            {
                snapshot.DiskSerial = "Unknown";
            }
        }

        /// <summary>
        /// 读取MAC地址
        /// </summary>
        /// <param name="snapshot">硬件信息快照</param>
        private static void ReadMacAddress(HardwareInfoSnapshot snapshot)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE AND MACAddress IS NOT NULL"))
                {
                    foreach (var item in searcher.Get())
                    {
                        snapshot.MacAddress = item["MACAddress"]?.ToString() ?? "Unknown";
                        break;
                    }
                }
            }
            catch
            {
                snapshot.MacAddress = "Unknown";
            }
        }

        /// <summary>
        /// 读取主板信息
        /// </summary>
        /// <param name="snapshot">硬件信息快照</param>
        private static void ReadMotherboardInfo(HardwareInfoSnapshot snapshot)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (var item in searcher.Get())
                    {
                        snapshot.MotherboardSerial = item["SerialNumber"]?.ToString()?.Trim() ?? "Unknown";
                        break;
                    }
                }
            }
            catch
            {
                snapshot.MotherboardSerial = "Unknown";
            }
        }

        /// <summary>
        /// 获取CPU ID
        /// </summary>
        /// <returns>CPU ID</returns>
        private static string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        return item["ProcessorId"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch
            {
                // 如果WMI失败，尝试使用其他方法
                try
                {
                    StringBuilder cpuId = new StringBuilder();
                    // 这里可以添加其他获取CPU ID的方法
                    return cpuId.ToString();
                }
                catch
                {
                    return "Unknown";
                }
            }

            return "Unknown";
        }
    }
}
