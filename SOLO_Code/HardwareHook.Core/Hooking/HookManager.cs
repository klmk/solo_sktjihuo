using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using MinHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Hooking
{
    /// <summary>
    /// Hook管理器
    /// </summary>
    public class HookManager
    {
        // 原始函数指针
        private static IntPtr _getSystemInfoPtr;
        private static IntPtr _getNativeSystemInfoPtr;
        private static IntPtr _getVolumeInformationWPtr;
        private static IntPtr _deviceIoControlPtr;
        private static IntPtr _getAdaptersInfoPtr;
        private static IntPtr _regOpenKeyExPtr;
        private static IntPtr _regQueryValueExPtr;

        // 原始函数委托
        private static DGetSystemInfo _originalGetSystemInfo;
        private static DGetNativeSystemInfo _originalGetNativeSystemInfo;
        private static DGetVolumeInformationW _originalGetVolumeInformationW;
        private static DDeviceIoControl _originalDeviceIoControl;
        private static DGetAdaptersInfo _originalGetAdaptersInfo;
        private static DRegOpenKeyEx _originalRegOpenKeyEx;
        private static DRegQueryValueEx _originalRegQueryValueEx;

        private static HardwareConfig _config;
        private static ILogger _logger;
        private static bool _isHooked;
        private static bool _minHookInitialized;
        private static WindowsVersion _windowsVersion;

        /// <summary>
        /// 是否已经安装Hook
        /// </summary>
        public static bool IsHooked => _isHooked;

        /// <summary>
        /// 安装所有Hook
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <param name="logger">日志记录器</param>
        public static void InstallAll(HardwareConfig config, ILogger logger)
        {
            if (_isHooked)
            {
                logger?.Warn("Hooks are already installed");
                return;
            }

            _config = config;
            _logger = logger;

            try
            {
                // 检测Windows版本
                _windowsVersion = DetectWindowsVersion();
                string windowsVersionStr = Environment.OSVersion.VersionString;
                _logger?.Info($"Windows version: {windowsVersionStr} ({_windowsVersion})");

                // 获取Hook策略
                HookStrategy strategy = GetHookStrategy(_windowsVersion);
                _logger?.Info($"Using hook strategy: Cpu={strategy.EnableCpuHooks}, Disk={strategy.EnableDiskHooks}, Mac={strategy.EnableMacHooks}, Motherboard={strategy.EnableMotherboardHooks}, ModernApi={strategy.UseModernApi}");

                // 初始化MinHook
                if (!_minHookInitialized)
                {
                    var result = MH_Initialize();
                    if (result != MH_STATUS.MH_OK)
                    {
                        throw new Exception($"Failed to initialize MinHook: {result}");
                    }
                    _minHookInitialized = true;
                    _logger?.Info("MinHook initialized successfully");
                }

                // 安装CPU相关Hook
                if (strategy.EnableCpuHooks)
                {
                    InstallCpuHooks();
                }
                else
                {
                    _logger?.Info("Skipping CPU hooks based on strategy");
                }

                // 安装硬盘相关Hook
                if (strategy.EnableDiskHooks)
                {
                    InstallDiskHooks();
                }
                else
                {
                    _logger?.Info("Skipping disk hooks based on strategy");
                }

                // 安装MAC相关Hook
                if (strategy.EnableMacHooks)
                {
                    InstallMacHooks();
                }
                else
                {
                    _logger?.Info("Skipping MAC hooks based on strategy");
                }

                // 安装主板相关Hook
                if (strategy.EnableMotherboardHooks)
                {
                    InstallMotherboardHooks();
                }
                else
                {
                    _logger?.Info("Skipping motherboard hooks based on strategy");
                }

                // 启用所有Hook
                var enableResult = MH_EnableHook(MH_ALL_HOOKS);
                if (enableResult != MH_STATUS.MH_OK)
                {
                    throw new Exception($"Failed to enable hooks: {enableResult}");
                }

                _isHooked = true;
                _logger?.Info("All hooks installed successfully");
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
                if (_isHooked)
                {
                    // 禁用所有Hook
                    var disableResult = MH_DisableHook(MH_ALL_HOOKS);
                    if (disableResult != MH_STATUS.MH_OK)
                    {
                        _logger?.Error($"Failed to disable hooks: {disableResult}");
                    }

                    // 移除所有Hook
                    var removeResult = MH_RemoveHook(MH_ALL_HOOKS);
                    if (removeResult != MH_STATUS.MH_OK)
                    {
                        _logger?.Error($"Failed to remove hooks: {removeResult}");
                    }

                    _isHooked = false;
                    _logger?.Info("All hooks uninstalled");
                }

                // 清理MinHook
                if (_minHookInitialized)
                {
                    var uninitResult = MH_Uninitialize();
                    if (uninitResult != MH_STATUS.MH_OK)
                    {
                        _logger?.Error($"Failed to uninitialize MinHook: {uninitResult}");
                    }
                    _minHookInitialized = false;
                }
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
                // 获取函数地址
                _getSystemInfoPtr = GetProcAddress("kernel32.dll", "GetSystemInfo");
                _getNativeSystemInfoPtr = GetProcAddress("kernel32.dll", "GetNativeSystemInfo");

                // 创建Hook
                var result1 = MH_CreateHook(_getSystemInfoPtr, typeof(HookManager).GetMethod("GetSystemInfoHook"), out IntPtr getSystemInfoTrampoline);
                var result2 = MH_CreateHook(_getNativeSystemInfoPtr, typeof(HookManager).GetMethod("GetNativeSystemInfoHook"), out IntPtr getNativeSystemInfoTrampoline);

                if (result1 != MH_STATUS.MH_OK || result2 != MH_STATUS.MH_OK)
                {
                    throw new Exception($"Failed to create CPU hooks: {result1}, {result2}");
                }

                // 获取原始函数委托
                _originalGetSystemInfo = Marshal.GetDelegateForFunctionPointer<DGetSystemInfo>(getSystemInfoTrampoline);
                _originalGetNativeSystemInfo = Marshal.GetDelegateForFunctionPointer<DGetNativeSystemInfo>(getNativeSystemInfoTrampoline);

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
                // 获取函数地址
                _getVolumeInformationWPtr = GetProcAddress("kernel32.dll", "GetVolumeInformationW");
                _deviceIoControlPtr = GetProcAddress("kernel32.dll", "DeviceIoControl");

                // 创建Hook
                var result1 = MH_CreateHook(_getVolumeInformationWPtr, typeof(HookManager).GetMethod("GetVolumeInformationWHook"), out IntPtr getVolumeInformationWTrampoline);
                var result2 = MH_CreateHook(_deviceIoControlPtr, typeof(HookManager).GetMethod("DeviceIoControlHook"), out IntPtr deviceIoControlTrampoline);

                if (result1 != MH_STATUS.MH_OK || result2 != MH_STATUS.MH_OK)
                {
                    throw new Exception($"Failed to create disk hooks: {result1}, {result2}");
                }

                // 获取原始函数委托
                _originalGetVolumeInformationW = Marshal.GetDelegateForFunctionPointer<DGetVolumeInformationW>(getVolumeInformationWTrampoline);
                _originalDeviceIoControl = Marshal.GetDelegateForFunctionPointer<DDeviceIoControl>(deviceIoControlTrampoline);

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
                // 获取函数地址
                _getAdaptersInfoPtr = GetProcAddress("iphlpapi.dll", "GetAdaptersInfo");

                // 创建Hook
                var result = MH_CreateHook(_getAdaptersInfoPtr, typeof(HookManager).GetMethod("GetAdaptersInfoHook"), out IntPtr getAdaptersInfoTrampoline);

                if (result != MH_STATUS.MH_OK)
                {
                    throw new Exception($"Failed to create MAC hooks: {result}");
                }

                // 获取原始函数委托
                _originalGetAdaptersInfo = Marshal.GetDelegateForFunctionPointer<DGetAdaptersInfo>(getAdaptersInfoTrampoline);

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
                // 获取函数地址
                _regOpenKeyExPtr = GetProcAddress("advapi32.dll", "RegOpenKeyExW");
                _regQueryValueExPtr = GetProcAddress("advapi32.dll", "RegQueryValueExW");

                // 创建Hook
                var result1 = MH_CreateHook(_regOpenKeyExPtr, typeof(HookManager).GetMethod("RegOpenKeyExHook"), out IntPtr regOpenKeyExTrampoline);
                var result2 = MH_CreateHook(_regQueryValueExPtr, typeof(HookManager).GetMethod("RegQueryValueExHook"), out IntPtr regQueryValueExTrampoline);

                if (result1 != MH_STATUS.MH_OK || result2 != MH_STATUS.MH_OK)
                {
                    throw new Exception($"Failed to create motherboard hooks: {result1}, {result2}");
                }

                // 获取原始函数委托
                _originalRegOpenKeyEx = Marshal.GetDelegateForFunctionPointer<DRegOpenKeyEx>(regOpenKeyExTrampoline);
                _originalRegQueryValueEx = Marshal.GetDelegateForFunctionPointer<DRegQueryValueEx>(regQueryValueExTrampoline);

                _logger?.Info("Motherboard hooks installed");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to install motherboard hooks", ex);
                // 不抛出异常，允许其他钩子继续安装
            }
        }

        /// <summary>
        /// 获取函数地址
        /// </summary>
        /// <param name="moduleName">模块名</param>
        /// <param name="functionName">函数名</param>
        /// <returns>函数指针</returns>
        private static IntPtr GetProcAddress(string moduleName, string functionName)
        {
            IntPtr hModule = GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero)
            {
                throw new Exception($"Failed to load module: {moduleName}");
            }

            IntPtr procAddress = GetProcAddress(hModule, functionName);
            if (procAddress == IntPtr.Zero)
            {
                throw new Exception($"Failed to find function: {functionName} in {moduleName}");
            }

            return procAddress;
        }

        // MinHook API
        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_Initialize();

        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_Uninitialize();

        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_CreateHook(IntPtr pTarget, IntPtr pDetour, out IntPtr ppOriginal);

        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_EnableHook(IntPtr pTarget);

        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_DisableHook(IntPtr pTarget);

        [DllImport("MinHook.x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern MH_STATUS MH_RemoveHook(IntPtr pTarget);

        // Windows API
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        // MinHook状态枚举
        private enum MH_STATUS
        {
            MH_OK = 0,
            MH_ERROR_ALREADY_INITIALIZED = 1,
            MH_ERROR_NOT_INITIALIZED = 2,
            MH_ERROR_ALREADY_CREATED = 3,
            MH_ERROR_NOT_CREATED = 4,
            MH_ERROR_ENABLED = 5,
            MH_ERROR_DISABLED = 6,
            MH_ERROR_NOT_EXECUTABLE = 7,
            MH_ERROR_UNSUPPORTED_FUNCTION = 8,
            MH_ERROR_MEMORY_ALLOC = 9,
            MH_ERROR_MEMORY_PROTECT = 10,
            MH_ERROR_MODULE_NOT_FOUND = 11,
            MH_ERROR_FUNCTION_NOT_FOUND = 12
        }

        // 特殊值
        private static readonly IntPtr MH_ALL_HOOKS = new IntPtr(-1);

        // Windows版本枚举
        private enum WindowsVersion
        {
            Unknown,
            Windows7,
            Windows8,
            Windows81,
            Windows10,
            Windows11
        }

        /// <summary>
        /// 检测Windows版本
        /// </summary>
        /// <returns>Windows版本</returns>
        private static WindowsVersion DetectWindowsVersion()
        {
            try
            {
                OperatingSystem os = Environment.OSVersion;
                Version version = os.Version;

                // Windows 11
                if (version.Major >= 10 && version.Build >= 22000)
                {
                    return WindowsVersion.Windows11;
                }
                // Windows 10
                else if (version.Major == 10 && version.Build < 22000)
                {
                    return WindowsVersion.Windows10;
                }
                // Windows 8.1
                else if (version.Major == 6 && version.Minor == 3)
                {
                    return WindowsVersion.Windows81;
                }
                // Windows 8
                else if (version.Major == 6 && version.Minor == 2)
                {
                    return WindowsVersion.Windows8;
                }
                // Windows 7
                else if (version.Major == 6 && version.Minor == 1)
                {
                    return WindowsVersion.Windows7;
                }
                else
                {
                    return WindowsVersion.Unknown;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to detect Windows version", ex);
                return WindowsVersion.Unknown;
            }
        }

        /// <summary>
        /// 根据Windows版本获取对应的Hook策略
        /// </summary>
        /// <param name="version">Windows版本</param>
        /// <returns>Hook策略</returns>
        private static HookStrategy GetHookStrategy(WindowsVersion version)
        {
            switch (version)
            {
                case WindowsVersion.Windows11:
                case WindowsVersion.Windows10:
                    return new HookStrategy
                    {
                        EnableCpuHooks = true,
                        EnableDiskHooks = true,
                        EnableMacHooks = true,
                        EnableMotherboardHooks = true,
                        UseModernApi = true
                    };
                case WindowsVersion.Windows81:
                case WindowsVersion.Windows8:
                    return new HookStrategy
                    {
                        EnableCpuHooks = true,
                        EnableDiskHooks = true,
                        EnableMacHooks = true,
                        EnableMotherboardHooks = true,
                        UseModernApi = false
                    };
                case WindowsVersion.Windows7:
                    return new HookStrategy
                    {
                        EnableCpuHooks = true,
                        EnableDiskHooks = true,
                        EnableMacHooks = false, // Windows 7的MAC API可能有兼容性问题
                        EnableMotherboardHooks = true,
                        UseModernApi = false
                    };
                default:
                    return new HookStrategy
                    {
                        EnableCpuHooks = true,
                        EnableDiskHooks = true,
                        EnableMacHooks = false,
                        EnableMotherboardHooks = true,
                        UseModernApi = false
                    };
            }
        }

        /// <summary>
        /// Hook策略
        /// </summary>
        private class HookStrategy
        {
            public bool EnableCpuHooks { get; set; }
            public bool EnableDiskHooks { get; set; }
            public bool EnableMacHooks { get; set; }
            public bool EnableMotherboardHooks { get; set; }
            public bool UseModernApi { get; set; }
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

        // 存储设备查询相关结构
        [StructLayout(LayoutKind.Sequential)]
        public struct STORAGE_PROPERTY_QUERY
        {
            public uint PropertyId;
            public uint QueryType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] AdditionalParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STORAGE_DEVICE_DESCRIPTOR
        {
            public uint Version;
            public uint Size;
            public byte DeviceType;
            public byte DeviceTypeModifier;
            public bool RemovableMedia;
            public bool CommandQueueing;
            public uint VendorIdOffset;
            public uint ProductIdOffset;
            public uint ProductRevisionOffset;
            public uint SerialNumberOffset;
            public uint MediaType;
            public uint MediaCharacteristics;
            public uint PhysicalMediaType;
            public uint BusType;
            public uint RawPropertiesLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] RawDeviceProperties;
        }

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
        public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
        public const uint StorageDeviceProperty = 0;
        public const uint PropertyStandardQuery = 0;

        #endregion

        #region Hook 回调函数

        /// <summary>
        /// GetSystemInfo Hook回调
        /// </summary>
        /// <param name="lpSystemInfo">系统信息结构体</param>
        public static void GetSystemInfoHook(ref SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                // 先调用原始API
                _originalGetSystemInfo(ref lpSystemInfo);

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
        public static void GetNativeSystemInfoHook(ref SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                // 先调用原始API
                _originalGetNativeSystemInfo(ref lpSystemInfo);

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
        public static bool GetVolumeInformationWHook(
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
                bool result = _originalGetVolumeInformationW(
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
        public static bool DeviceIoControlHook(
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
                bool result = _originalDeviceIoControl(
                    hDevice,
                    dwIoControlCode,
                    lpInBuffer,
                    nInBufferSize,
                    lpOutBuffer,
                    nOutBufferSize,
                    ref lpBytesReturned,
                    lpOverlapped);

                // 模拟硬盘序列号
                if (_config != null && _config.Disk != null && !string.IsNullOrEmpty(_config.Disk.Serial))
                {
                    // 处理硬盘序列号相关的IO控制代码
                    if (dwIoControlCode == IOCTL_STORAGE_QUERY_PROPERTY && lpOutBuffer != IntPtr.Zero && lpBytesReturned > 0)
                    {
                        try
                        {
                            // 解析STORAGE_DEVICE_DESCRIPTOR
                            STORAGE_DEVICE_DESCRIPTOR descriptor = Marshal.PtrToStructure<STORAGE_DEVICE_DESCRIPTOR>(lpOutBuffer);
                            
                            // 检查是否包含序列号
                            if (descriptor.SerialNumberOffset > 0 && descriptor.SerialNumberOffset < lpBytesReturned)
                            {
                                // 计算序列号在缓冲区中的位置
                                IntPtr serialPtr = new IntPtr(lpOutBuffer.ToInt64() + descriptor.SerialNumberOffset);
                                
                                // 复制模拟的序列号
                                byte[] serialBytes = System.Text.Encoding.ASCII.GetBytes(_config.Disk.Serial + '\0');
                                int serialLength = Math.Min(serialBytes.Length, (int)(lpBytesReturned - descriptor.SerialNumberOffset));
                                try
                                {
                                    Marshal.Copy(serialBytes, 0, serialPtr, serialLength);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.Debug("Failed to copy serial number bytes", ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.Debug("Failed to modify storage device descriptor", ex);
                        }
                    }
                }

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
        public static int GetAdaptersInfoHook(
            IntPtr pAdapterInfo,
            ref int pOutBufLen)
        {
            try
            {
                // 先调用原始API
                int result = _originalGetAdaptersInfo(pAdapterInfo, ref pOutBufLen);

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
                                    try
                                    {
                                        Marshal.Copy(macBytes, 0, new IntPtr(currentAdapter.ToInt64() + Marshal.OffsetOf<IP_ADAPTER_INFO>("Address").ToInt64()), 6);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.Debug("Failed to copy MAC address bytes", ex);
                                    }
                                    break;
                                }
                                currentAdapter = adapter.Next;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Debug("Failed to modify MAC address", ex);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetAdaptersInfoHook failed", ex);
                return -1;
            }
        }

        // 存储主板相关的注册表路径
        private static readonly HashSet<string> _motherboardRegistryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HARDWARE\\DESCRIPTION\\System",
            "HARDWARE\\DESCRIPTION\\System\\BIOS",
            "HARDWARE\\DEVICEMAP\\Scsi\\Scsi Port 0\\Scsi Bus 0\\Target Id 0\\Logical Unit Id 0",
            "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
            "SYSTEM\\CurrentControlSet\\Control\\SystemInformation"
        };

        /// <summary>
        /// RegOpenKeyEx Hook回调
        /// </summary>
        /// <param name="hKey">注册表键句柄</param>
        /// <param name="lpSubKey">子键名称</param>
        /// <param name="ulOptions">选项</param>
        /// <param name="samDesired">访问权限</param>
        /// <param name="phkResult">结果键句柄</param>
        /// <returns>操作结果</returns>
        public static int RegOpenKeyExHook(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            ref IntPtr phkResult)
        {
            try
            {
                // 先调用原始API
                int result = _originalRegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, ref phkResult);

                // 记录访问主板相关注册表的操作
                if (result == 0 && lpSubKey != null && _motherboardRegistryPaths.Any(path => lpSubKey.Contains(path)))
                {
                    _logger?.Debug($"Accessed motherboard registry path: {lpSubKey}");
                }

                return result;
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
        public static int RegQueryValueExHook(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            IntPtr lpData,
            ref uint lpcbData)
        {
            try
            {
                // 检查是否是读取主板相关信息的注册表项
                if (_config != null)
                {
                    // 模拟主板序列号
                    if (_config.Motherboard != null && !string.IsNullOrEmpty(_config.Motherboard.Serial))
                    {
                        // 常见的主板序列号注册表值
                        string[] serialValueNames = {
                            "SerialNumber", "BIOSSerialNumber", "BaseBoardSerialNumber",
                            "SystemBiosVersion", "BiosVersion", "BIOSVersion"
                        };

                        if (serialValueNames.Any(name => name.Equals(lpValueName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return SimulateRegistryStringValue(_config.Motherboard.Serial, ref lpType, lpData, ref lpcbData);
                        }
                    }

                    // 模拟CPU信息
                    if (_config.Cpu != null && !string.IsNullOrEmpty(_config.Cpu.Model))
                    {
                        // 常见的CPU型号注册表值
                        string[] cpuValueNames = {
                            "ProcessorNameString", "VendorIdentifier", "Identifier"
                        };

                        if (cpuValueNames.Any(name => name.Equals(lpValueName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return SimulateRegistryStringValue(_config.Cpu.Model, ref lpType, lpData, ref lpcbData);
                        }
                    }
                }

                // 先调用原始API
                return _originalRegQueryValueEx(hKey, lpValueName, lpReserved, ref lpType, lpData, ref lpcbData);
            }
            catch (Exception ex)
            {
                _logger?.Error("RegQueryValueExHook failed", ex);
                return Marshal.GetLastWin32Error();
            }
        }

        /// <summary>
        /// 模拟注册表字符串值
        /// </summary>
        /// <param name="value">模拟值</param>
        /// <param name="lpType">值类型</param>
        /// <param name="lpData">数据缓冲区</param>
        /// <param name="lpcbData">数据缓冲区大小</param>
        /// <returns>操作结果</returns>
        private static int SimulateRegistryStringValue(string value, ref uint lpType, IntPtr lpData, ref uint lpcbData)
        {
            try
            {
                // 模拟字符串值
                byte[] valueBytes = System.Text.Encoding.Unicode.GetBytes(value + '\0');
                uint requiredSize = (uint)valueBytes.Length;

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
                    Marshal.Copy(valueBytes, 0, lpData, valueBytes.Length);
                    lpcbData = requiredSize;
                    lpType = 1; // REG_SZ
                    return 0;
                }
                else
                {
                    // 缓冲区大小不足
                    lpcbData = requiredSize;
                    return 234; // ERROR_MORE_DATA
                }
            }
            catch (Exception ex)
            {
                _logger?.Debug("Failed to simulate registry string value", ex);
                return Marshal.GetLastWin32Error();
            }
        }

        #endregion
    }
}
