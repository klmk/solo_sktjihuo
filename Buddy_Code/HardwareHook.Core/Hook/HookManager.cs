using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EasyHook;
using HardwareHook.Core.Config;
using HardwareHook.Core.Logging;
using HardwareHook.Core.Native;

namespace HardwareHook.Core.Hook
{
    /// <summary>
    /// Hook管理器
    /// </summary>
    public static class HookManager
    {
        private static readonly object _lock = new object();
        private static readonly List<LocalHook> _hooks = new List<LocalHook>();
        private static bool _isInitialized = false;
        private static HardwareConfig _config;
        
        /// <summary>
        /// 安装所有Hook
        /// </summary>
        public static void InstallAll(HardwareConfig config)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    Logger.Warning("Hook已经初始化，跳过", "HookManager");
                    return;
                }
                
                _config = config ?? throw new ArgumentNullException(nameof(config));
                
                try
                {
                    // 安装CPU信息Hook
                    InstallCpuHooks();
                    
                    // 安装硬盘信息Hook
                    InstallDiskHooks();
                    
                    // 安装MAC地址Hook
                    InstallMacHooks();
                    
                    // 安装主板信息Hook
                    InstallMotherboardHooks();
                    
                    _isInitialized = true;
                    Logger.Info("所有Hook安装成功", "HookManager");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Hook安装失败: {ex.Message}", "HookManager");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// 卸载所有Hook
        /// </summary>
        public static void UninstallAll()
        {
            lock (_lock)
            {
                foreach (var hook in _hooks)
                {
                    try
                    {
                        hook.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Hook卸载失败: {ex.Message}", "HookManager");
                    }
                }
                
                _hooks.Clear();
                _isInitialized = false;
                Logger.Info("所有Hook已卸载", "HookManager");
            }
        }
        
        /// <summary>
        /// 安装CPU信息Hook
        /// </summary>
        private static void InstallCpuHooks()
        {
            try
            {
                var hook1 = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetSystemInfo"),
                    new GetSystemInfoDelegate(GetSystemInfo_Hooked),
                    this);
                hook1.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook1);
                
                var hook2 = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetNativeSystemInfo"),
                    new GetNativeSystemInfoDelegate(GetNativeSystemInfo_Hooked),
                    this);
                hook2.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook2);
                
                Logger.Info("CPU信息Hook安装成功", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"CPU信息Hook安装失败: {ex.Message}", "HookManager");
                throw;
            }
        }
        
        /// <summary>
        /// 安装硬盘信息Hook
        /// </summary>
        private static void InstallDiskHooks()
        {
            try
            {
                var hook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "GetVolumeInformationW"),
                    new GetVolumeInformationWDelegate(GetVolumeInformationW_Hooked),
                    this);
                hook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook);
                
                Logger.Info("硬盘信息Hook安装成功", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"硬盘信息Hook安装失败: {ex.Message}", "HookManager");
                throw;
            }
        }
        
        /// <summary>
        /// 安装MAC地址Hook
        /// </summary>
        private static void InstallMacHooks()
        {
            try
            {
                var hook = LocalHook.Create(
                    LocalHook.GetProcAddress("iphlpapi.dll", "GetAdaptersInfo"),
                    new GetAdaptersInfoDelegate(GetAdaptersInfo_Hooked),
                    this);
                hook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook);
                
                Logger.Info("MAC地址Hook安装成功", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"MAC地址Hook安装失败: {ex.Message}", "HookManager");
                throw;
            }
        }
        
        /// <summary>
        /// 安装主板信息Hook
        /// </summary>
        private static void InstallMotherboardHooks()
        {
            try
            {
                var hook1 = LocalHook.Create(
                    LocalHook.GetProcAddress("advapi32.dll", "RegOpenKeyExW"),
                    new RegOpenKeyExWDelegate(RegOpenKeyExW_Hooked),
                    this);
                hook1.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook1);
                
                var hook2 = LocalHook.Create(
                    LocalHook.GetProcAddress("advapi32.dll", "RegQueryValueExW"),
                    new RegQueryValueExWDelegate(RegQueryValueExW_Hooked),
                    this);
                hook2.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _hooks.Add(hook2);
                
                Logger.Info("主板信息Hook安装成功", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"主板信息Hook安装失败: {ex.Message}", "HookManager");
                throw;
            }
        }
        
        #region Hook委托定义
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void GetSystemInfoDelegate(out NativeApi.SYSTEM_INFO lpSystemInfo);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void GetNativeSystemInfoDelegate(out NativeApi.SYSTEM_INFO lpSystemInfo);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private delegate bool GetVolumeInformationWDelegate(
            string lpRootPathName,
            IntPtr lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            IntPtr lpFileSystemNameBuffer,
            int nFileSystemNameSize);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetAdaptersInfoDelegate(IntPtr pAdapterInfo, ref int pOutBufLen);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private delegate int RegOpenKeyExWDelegate(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            uint samDesired,
            out IntPtr phkResult);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private delegate int RegQueryValueExWDelegate(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            out uint lpType,
            IntPtr lpData,
            ref uint lpcbData);
        
        #endregion
        
        #region Hook实现
        
        private static void GetSystemInfo_Hooked(out NativeApi.SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                // 调用原始API
                NativeApi.GetSystemInfo(out lpSystemInfo);
                
                // 应用配置中的CPU核心数
                if (_config?.Cpu != null && _config.Cpu.CoreCount > 0)
                {
                    lpSystemInfo.dwNumberOfProcessors = (uint)_config.Cpu.CoreCount;
                }
                
                Logger.Debug($"GetSystemInfo Hooked: CoreCount={lpSystemInfo.dwNumberOfProcessors}", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"GetSystemInfo Hook异常: {ex.Message}", "HookManager");
                NativeApi.GetSystemInfo(out lpSystemInfo);
            }
        }
        
        private static void GetNativeSystemInfo_Hooked(out NativeApi.SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                NativeApi.GetNativeSystemInfo(out lpSystemInfo);
                
                if (_config?.Cpu != null && _config.Cpu.CoreCount > 0)
                {
                    lpSystemInfo.dwNumberOfProcessors = (uint)_config.Cpu.CoreCount;
                }
                
                Logger.Debug($"GetNativeSystemInfo Hooked: CoreCount={lpSystemInfo.dwNumberOfProcessors}", "HookManager");
            }
            catch (Exception ex)
            {
                Logger.Error($"GetNativeSystemInfo Hook异常: {ex.Message}", "HookManager");
                NativeApi.GetNativeSystemInfo(out lpSystemInfo);
            }
        }
        
        private static bool GetVolumeInformationW_Hooked(
            string lpRootPathName,
            IntPtr lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            IntPtr lpFileSystemNameBuffer,
            int nFileSystemNameSize)
        {
            try
            {
                bool result = NativeApi.GetVolumeInformationW(
                    lpRootPathName,
                    lpVolumeNameBuffer,
                    nVolumeNameSize,
                    out lpVolumeSerialNumber,
                    out lpMaximumComponentLength,
                    out lpFileSystemFlags,
                    lpFileSystemNameBuffer,
                    nFileSystemNameSize);
                
                // 应用配置的硬盘序列号
                if (result && _config?.Disk != null && !string.IsNullOrEmpty(_config.Disk.Serial))
                {
                    // 将序列号转换为数字（简化处理）
                    uint serial = 0;
                    foreach (char c in _config.Disk.Serial)
                    {
                        serial = serial * 31 + c;
                    }
                    lpVolumeSerialNumber = serial;
                }
                
                Logger.Debug($"GetVolumeInformationW Hooked: Serial={lpVolumeSerialNumber:X}", "HookManager");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetVolumeInformationW Hook异常: {ex.Message}", "HookManager");
                return NativeApi.GetVolumeInformationW(
                    lpRootPathName,
                    lpVolumeNameBuffer,
                    nVolumeNameSize,
                    out lpVolumeSerialNumber,
                    out lpMaximumComponentLength,
                    out lpFileSystemFlags,
                    lpFileSystemNameBuffer,
                    nFileSystemNameSize);
            }
        }
        
        private static int GetAdaptersInfo_Hooked(IntPtr pAdapterInfo, ref int pOutBufLen)
        {
            try
            {
                int result = NativeApi.GetAdaptersInfo(pAdapterInfo, ref pOutBufLen);
                
                // 如果调用成功且配置中有MAC地址，则修改返回的数据
                if (result == 0 && pAdapterInfo != IntPtr.Zero && _config?.Mac != null && !string.IsNullOrEmpty(_config.Mac.Address))
                {
                    try
                    {
                        // 解析MAC地址
                        string[] macParts = _config.Mac.Address.Split(':', '-');
                        if (macParts.Length == 6)
                        {
                            byte[] macBytes = new byte[6];
                            for (int i = 0; i < 6; i++)
                            {
                                macBytes[i] = Convert.ToByte(macParts[i], 16);
                            }
                            
                            // 修改第一个适配器的MAC地址
                            var adapterInfo = Marshal.PtrToStructure<NativeApi.IP_ADAPTER_INFO>(pAdapterInfo);
                            adapterInfo.Address = macBytes;
                            adapterInfo.AddressLength = 6;
                            Marshal.StructureToPtr(adapterInfo, pAdapterInfo, false);
                            
                            Logger.Debug($"GetAdaptersInfo Hooked: MAC={_config.Mac.Address}", "HookManager");
                        }
                    }
                    catch (Exception macEx)
                    {
                        Logger.Warning($"MAC地址解析失败: {macEx.Message}", "HookManager");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetAdaptersInfo Hook异常: {ex.Message}", "HookManager");
                return NativeApi.GetAdaptersInfo(pAdapterInfo, ref pOutBufLen);
            }
        }
        
        private static int RegOpenKeyExW_Hooked(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            uint samDesired,
            out IntPtr phkResult)
        {
            try
            {
                int result = NativeApi.RegOpenKeyExW(hKey, lpSubKey, ulOptions, samDesired, out phkResult);
                
                // 记录主板信息相关的注册表访问
                if (lpSubKey != null && lpSubKey.Contains("SYSTEM\\CurrentControlSet\\Enum"))
                {
                    Logger.Debug($"RegOpenKeyExW Hooked: {lpSubKey}", "HookManager");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"RegOpenKeyExW Hook异常: {ex.Message}", "HookManager");
                return NativeApi.RegOpenKeyExW(hKey, lpSubKey, ulOptions, samDesired, out phkResult);
            }
        }
        
        private static int RegQueryValueExW_Hooked(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            out uint lpType,
            IntPtr lpData,
            ref uint lpcbData)
        {
            try
            {
                int result = NativeApi.RegQueryValueExW(hKey, lpValueName, lpReserved, out lpType, lpData, ref lpcbData);
                
                // 模拟主板序列号
                if (result == 0 && lpValueName != null && 
                    (lpValueName.Equals("SystemBiosVersion", StringComparison.OrdinalIgnoreCase) ||
                     lpValueName.Equals("SystemBiosDate", StringComparison.OrdinalIgnoreCase)) &&
                    _config?.Motherboard != null)
                {
                    // 这里简化处理，实际应用中需要更复杂的逻辑
                    Logger.Debug($"RegQueryValueExW Hooked: {lpValueName} (主板信息模拟)", "HookManager");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"RegQueryValueExW Hook异常: {ex.Message}", "HookManager");
                return NativeApi.RegQueryValueExW(hKey, lpValueName, lpReserved, out lpType, lpData, ref lpcbData);
            }
        }
        
        #endregion
    }
}
