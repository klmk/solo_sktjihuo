using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using EasyHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Hooking
{
    /// <summary>
    /// Hook管理器
    /// </summary>
    public class HookManager
    {
        private static LocalHook _getSystemInfoHook;
        private static LocalHook _getNativeSystemInfoHook;
        private static LocalHook _getVolumeInformationWHook;
        private static LocalHook _deviceIoControlHook;
        private static LocalHook _getAdaptersInfoHook;
        private static LocalHook _regOpenKeyExHook;
        private static LocalHook _regQueryValueExHook;

        private static HardwareConfig _config;
        private static ILogger _logger;

        /// <summary>
        /// 安装所有Hook
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <param name="logger">日志记录器</param>
        public static void InstallAll(HardwareConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;

            try
            {
                // 安装CPU相关Hook
                InstallCpuHooks();

                // 安装硬盘相关Hook
                InstallDiskHooks();

                // 安装MAC相关Hook
                InstallMacHooks();

                // 安装主板相关Hook
                InstallMotherboardHooks();
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install hooks", ex);
                UninstallAll();
                throw;
            }
        }

        /// <summary>
        /// 卸载所有Hook
        /// </summary>
        public static void UninstallAll()
        {
            try
            {
                _getSystemInfoHook?.Dispose();
                _getNativeSystemInfoHook?.Dispose();
                _getVolumeInformationWHook?.Dispose();
                _deviceIoControlHook?.Dispose();
                _getAdaptersInfoHook?.Dispose();
                _regOpenKeyExHook?.Dispose();
                _regQueryValueExHook?.Dispose();

                _logger?.Info("All hooks uninstalled");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to uninstall hooks", ex);
            }
        }

        /// <summary>
        /// 安装CPU相关Hook
        /// </summary>
        private static void InstallCpuHooks()
        {
            try
            {
                _getSystemInfoHook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetSystemInfo"),
                    new DGetSystemInfo(GetSystemInfoHook),
                    null);

                _getNativeSystemInfoHook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetNativeSystemInfo"),
                    new DGetNativeSystemInfo(GetNativeSystemInfoHook),
                    null);

                _getSystemInfoHook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _getNativeSystemInfoHook.ThreadACL.SetInclusiveACL(new int[] { 0 });

                _logger?.Info("CPU hooks installed");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install CPU hooks", ex);
                throw;
            }
        }

        /// <summary>
        /// 安装硬盘相关Hook
        /// </summary>
        private static void InstallDiskHooks()
        {
            try
            {
                _getVolumeInformationWHook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetVolumeInformationW"),
                    new DGetVolumeInformationW(GetVolumeInformationWHook),
                    null);

                _deviceIoControlHook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "DeviceIoControl"),
                    new DDeviceIoControl(DeviceIoControlHook),
                    null);

                _getVolumeInformationWHook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _deviceIoControlHook.ThreadACL.SetInclusiveACL(new int[] { 0 });

                _logger?.Info("Disk hooks installed");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install disk hooks", ex);
                throw;
            }
        }

        /// <summary>
        /// 安装MAC相关Hook
        /// </summary>
        private static void InstallMacHooks()
        {
            try
            {
                _getAdaptersInfoHook = LocalHook.Create(
                    LocalHook.GetProcAddress("iphlpapi.dll", "GetAdaptersInfo"),
                    new DGetAdaptersInfo(GetAdaptersInfoHook),
                    null);

                _getAdaptersInfoHook.ThreadACL.SetInclusiveACL(new int[] { 0 });

                _logger?.Info("MAC hooks installed");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install MAC hooks", ex);
                // 不抛出异常，允许其他钩子继续安装
            }
        }

        /// <summary>
        /// 安装主板相关Hook
        /// </summary>
        private static void InstallMotherboardHooks()
        {
            try
            {
                _regOpenKeyExHook = LocalHook.Create(
                    LocalHook.GetProcAddress("advapi32.dll", "RegOpenKeyExW"),
                    new DRegOpenKeyEx(RegOpenKeyExHook),
                    null);

                _regQueryValueExHook = LocalHook.Create(
                    LocalHook.GetProcAddress("advapi32.dll", "RegQueryValueExW"),
                    new DRegQueryValueEx(RegQueryValueExHook),
                    null);

                _regOpenKeyExHook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _regQueryValueExHook.ThreadACL.SetInclusiveACL(new int[] { 0 });

                _logger?.Info("Motherboard hooks installed");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install motherboard hooks", ex);
                // 不抛出异常，允许其他钩子继续安装
            }
        }

        #region API 定义

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DGetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DGetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate bool DGetVolumeInformationW(
            string lpRootPathName,
            string lpVolumeNameBuffer,
            int nVolumeNameSize,
            ref uint lpVolumeSerialNumber,
            ref uint lpMaximumComponentLength,
            ref uint lpFileSystemFlags,
            string lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetVolumeInformationW(
            string lpRootPathName,
            string lpVolumeNameBuffer,
            int nVolumeNameSize,
            ref uint lpVolumeSerialNumber,
            ref uint lpMaximumComponentLength,
            ref uint lpFileSystemFlags,
            string lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool DDeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct IP_ADAPTER_INFO
        {
            public int Length;
            public IntPtr Next;
            public uint ComboIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string AdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Description;
            public uint AddressLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Address;
            public uint Index;
            public uint Type;
            public uint DhcpEnabled;
            public IntPtr CurrentIpAddress;
            public IP_ADDR_STRING IpAddressList;
            public IP_ADDR_STRING GatewayList;
            public IP_ADDR_STRING DhcpServer;
            public bool HaveWins;
            public IP_ADDR_STRING PrimaryWinsServer;
            public IP_ADDR_STRING SecondaryWinsServer;
            public uint LeaseObtained;
            public uint LeaseExpires;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct IP_ADDR_STRING
        {
            public IntPtr Next;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpAddress;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpMask;
            public uint Context;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DGetAdaptersInfo(
            IntPtr pAdapterInfo,
            ref int pOutBufLen);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdaptersInfo(
            IntPtr pAdapterInfo,
            ref int pOutBufLen);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate int DRegOpenKeyEx(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            ref IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            ref IntPtr phkResult);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate int DRegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            IntPtr lpData,
            ref uint lpcbData);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int RegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            IntPtr lpData,
            ref uint lpcbData);

        public const int HKEY_LOCAL_MACHINE = unchecked((int)0x80000002);
        public const int KEY_READ = 0x20019;

        #endregion

        #region Hook 回调函数

        /// <summary>
        /// GetSystemInfo Hook回调
        /// </summary>
        /// <param name="lpSystemInfo">系统信息结构体</param>
        private static void GetSystemInfoHook(ref SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                // 先调用原始API
                GetSystemInfo(ref lpSystemInfo);

                // 模拟CPU核心数
                if (_config != null && _config.Cpu != null)
                {
                    lpSystemInfo.dwNumberOfProcessors = (uint)_config.Cpu.CoreCount;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("GetSystemInfoHook failed", ex);
            }
        }

        /// <summary>
        /// GetNativeSystemInfo Hook回调
        /// </summary>
        /// <param name="lpSystemInfo">系统信息结构体</param>
        private static void GetNativeSystemInfoHook(ref SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                // 先调用原始API
                GetNativeSystemInfo(ref lpSystemInfo);

                // 模拟CPU核心数
                if (_config != null && _config.Cpu != null)
                {
                    lpSystemInfo.dwNumberOfProcessors = (uint)_config.Cpu.CoreCount;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("GetNativeSystemInfoHook failed", ex);
            }
        }

        /// <summary>
        /// GetVolumeInformationW Hook回调
        /// </summary>
        /// <param name="lpRootPathName">根路径</param>
        /// <param name="lpVolumeNameBuffer">卷名缓冲区</param>
        /// <param name="nVolumeNameSize">卷名缓冲区大小</param>
        /// <param name="lpVolumeSerialNumber">卷序列号</param>
        /// <param name="lpMaximumComponentLength">最大组件长度</param>
        /// <param name="lpFileSystemFlags">文件系统标志</param>
        /// <param name="lpFileSystemNameBuffer">文件系统名称缓冲区</param>
        /// <param name="nFileSystemNameSize">文件系统名称缓冲区大小</param>
        /// <returns>操作结果</returns>
        private static bool GetVolumeInformationWHook(
            string lpRootPathName,
            string lpVolumeNameBuffer,
            int nVolumeNameSize,
            ref uint lpVolumeSerialNumber,
            ref uint lpMaximumComponentLength,
            ref uint lpFileSystemFlags,
            string lpFileSystemNameBuffer,
            int nFileSystemNameSize)
        {
            try
            {
                // 先调用原始API
                bool result = GetVolumeInformationW(
                    lpRootPathName,
                    lpVolumeNameBuffer,
                    nVolumeNameSize,
                    ref lpVolumeSerialNumber,
                    ref lpMaximumComponentLength,
                    ref lpFileSystemFlags,
                    lpFileSystemNameBuffer,
                    nFileSystemNameSize);

                // 模拟硬盘序列号
                if (_config != null && _config.Disk != null && !string.IsNullOrEmpty(_config.Disk.Serial))
                {
                    // 尝试将配置的序列号转换为uint
                    try
                    {
                        if (_config.Disk.Serial.Length >= 8)
                        {
                            string serialHex = _config.Disk.Serial.Substring(0, 8);
                            if (uint.TryParse(serialHex, System.Globalization.NumberStyles.HexNumber, null, out uint serial))
                            {
                                lpVolumeSerialNumber = serial;
                            }
                        }
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetVolumeInformationWHook failed", ex);
                return false;
            }
        }

        /// <summary>
        /// DeviceIoControl Hook回调
        /// </summary>
        /// <param name="hDevice">设备句柄</param>
        /// <param name="dwIoControlCode">IO控制代码</param>
        /// <param name="lpInBuffer">输入缓冲区</param>
        /// <param name="nInBufferSize">输入缓冲区大小</param>
        /// <param name="lpOutBuffer">输出缓冲区</param>
        /// <param name="nOutBufferSize">输出缓冲区大小</param>
        /// <param name="lpBytesReturned">返回字节数</param>
        /// <param name="lpOverlapped">重叠结构</param>
        /// <returns>操作结果</returns>
        private static bool DeviceIoControlHook(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped)
        {
            try
            {
                // 先调用原始API
                bool result = DeviceIoControl(
                    hDevice,
                    dwIoControlCode,
                    lpInBuffer,
                    nInBufferSize,
                    lpOutBuffer,
                    nOutBufferSize,
                    ref lpBytesReturned,
                    lpOverlapped);

                // 这里可以添加硬盘信息模拟逻辑

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("DeviceIoControlHook failed", ex);
                return false;
            }
        }

        /// <summary>
        /// GetAdaptersInfo Hook回调
        /// </summary>
        /// <param name="pAdapterInfo">适配器信息指针</param>
        /// <param name="pOutBufLen">输出缓冲区长度</param>
        /// <returns>操作结果</returns>
        private static int GetAdaptersInfoHook(
            IntPtr pAdapterInfo,
            ref int pOutBufLen)
        {
            try
            {
                // 先调用原始API
                int result = GetAdaptersInfo(pAdapterInfo, ref pOutBufLen);

                // 模拟MAC地址
                if (result == 0 && pAdapterInfo != IntPtr.Zero && _config != null && _config.Mac != null && !string.IsNullOrEmpty(_config.Mac.Address))
                {
                    try
                    {
                        // 解析MAC地址
                        string[] macParts = _config.Mac.Address.Split(':');
                        if (macParts.Length == 6)
                        {
                            byte[] macBytes = new byte[6];
                            for (int i = 0; i < 6; i++)
                            {
                                macBytes[i] = byte.Parse(macParts[i], System.Globalization.NumberStyles.HexNumber);
                            }

                            // 遍历适配器列表，修改第一个物理适配器的MAC地址
                            IntPtr currentAdapter = pAdapterInfo;
                            while (currentAdapter != IntPtr.Zero)
                            {
                                IP_ADAPTER_INFO adapter = Marshal.PtrToStructure<IP_ADAPTER_INFO>(currentAdapter);
                                if (adapter.AddressLength == 6)
                                {
                                    // 复制模拟的MAC地址
                                    Marshal.Copy(macBytes, 0, new IntPtr(currentAdapter.ToInt64() + Marshal.OffsetOf<IP_ADAPTER_INFO>("Address").ToInt64()), 6);
                                    break;
                                }
                                currentAdapter = adapter.Next;
                            }
                        }
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetAdaptersInfoHook failed", ex);
                return -1;
            }
        }

        /// <summary>
        /// RegOpenKeyEx Hook回调
        /// </summary>
        /// <param name="hKey">注册表键句柄</param>
        /// <param name="lpSubKey">子键名称</param>
        /// <param name="ulOptions">选项</param>
        /// <param name="samDesired">访问权限</param>
        /// <param name="phkResult">结果键句柄</param>
        /// <returns>操作结果</returns>
        private static int RegOpenKeyExHook(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            ref IntPtr phkResult)
        {
            try
            {
                // 先调用原始API
                return RegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, ref phkResult);
            }
            catch (Exception ex)
            {
                _logger?.Error("RegOpenKeyExHook failed", ex);
                return Marshal.GetLastWin32Error();
            }
        }

        /// <summary>
        /// RegQueryValueEx Hook回调
        /// </summary>
        /// <param name="hKey">注册表键句柄</param>
        /// <param name="lpValueName">值名称</param>
        /// <param name="lpReserved">保留参数</param>
        /// <param name="lpType">值类型</param>
        /// <param name="lpData">数据缓冲区</param>
        /// <param name="lpcbData">数据缓冲区大小</param>
        /// <returns>操作结果</returns>
        private static int RegQueryValueExHook(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            IntPtr lpData,
            ref uint lpcbData)
        {
            try
            {
                // 先调用原始API
                int result = RegQueryValueEx(hKey, lpValueName, lpReserved, ref lpType, lpData, ref lpcbData);

                // 模拟主板序列号
                if (_config != null && _config.Motherboard != null && !string.IsNullOrEmpty(_config.Motherboard.Serial))
                {
                    // 检查是否是读取主板序列号的注册表项
                    // 常见的主板序列号注册表项
                    if (lpValueName == "SerialNumber" || lpValueName == "BIOSSerialNumber")
                    {
                        try
                        {
                            // 模拟主板序列号
                            byte[] serialBytes = System.Text.Encoding.Unicode.GetBytes(_config.Motherboard.Serial + '\0');
                            uint requiredSize = (uint)serialBytes.Length;

                            if (lpData == IntPtr.Zero)
                            {
                                // 第一次调用，返回所需大小
                                lpcbData = requiredSize;
                                lpType = 1; // REG_SZ
                                return 0;
                            }
                            else if (lpcbData >= requiredSize)
                            {
                                // 第二次调用，复制数据
                                Marshal.Copy(serialBytes, 0, lpData, serialBytes.Length);
                                lpcbData = requiredSize;
                                lpType = 1; // REG_SZ
                                return 0;
                            }
                        }
                        catch { }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("RegQueryValueExHook failed", ex);
                return Marshal.GetLastWin32Error();
            }
        }

        #endregion
    }
}
