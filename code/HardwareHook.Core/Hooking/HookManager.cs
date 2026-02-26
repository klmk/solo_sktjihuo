using System;
using System.Runtime.InteropServices;
using EasyHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Hooking
{
    public sealed class HookManager : IDisposable
    {
        private readonly HardwareConfig _config;
        private readonly ILogger _logger;

        private bool _installed;

        private LocalHook? _getSystemInfoHook;
        private GetSystemInfoDelegate? _getSystemInfoOriginal;

        public HookManager(HardwareConfig config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void InstallAll()
        {
            if (_installed)
            {
                return;
            }

            try
            {
                InstallSystemHooks();
                // 预留：后续在此处调用 InstallDiskHooks / InstallMacHooks / InstallMotherboardHooks

                _installed = true;
                _logger.Info("所有计划中的 Hook 已尝试安装。", "HookManager");
            }
            catch (Exception ex)
            {
                _logger.Error("安装 Hook 时发生未处理异常。", ex, "HookManager");
            }
        }

        public void UninstallAll()
        {
            if (!_installed)
            {
                return;
            }

            try
            {
                try
                {
                    _getSystemInfoHook?.Dispose();
                    _getSystemInfoHook = null;
                    _getSystemInfoOriginal = null;
                }
                catch (Exception ex)
                {
                    _logger.Error("卸载 GetSystemInfo Hook 时发生异常。", ex, "HookManager");
                }
            }
            finally
            {
                _installed = false;
                _logger.Info("所有 Hook 已卸载完成（或部分卸载失败，详见日志）。", "HookManager");
            }
        }

        private void InstallSystemHooks()
        {
            try
            {
                var procAddress = LocalHook.GetProcAddress("kernel32.dll", "GetSystemInfo");

                _getSystemInfoOriginal = (GetSystemInfoDelegate)Marshal.GetDelegateForFunctionPointer(
                    procAddress,
                    typeof(GetSystemInfoDelegate));

                _getSystemInfoHook = LocalHook.Create(
                    procAddress,
                    new GetSystemInfoDelegate(GetSystemInfo_Hooked),
                    null);

                // 包含所有线程
                _getSystemInfoHook.ThreadACL.SetInclusiveACL(new[] { 0 });

                _logger.Info("GetSystemInfo Hook 安装成功。", "HookManager");
            }
            catch (Exception ex)
            {
                _logger.Error("安装 GetSystemInfo Hook 失败。", ex, "HookManager");
            }
        }

        private void GetSystemInfo_Hooked(out SYSTEM_INFO lpSystemInfo)
        {
            try
            {
                if (_getSystemInfoOriginal == null)
                {
                    // 原函数未正确绑定，降级为直接调用 P/Invoke
                    GetSystemInfoNative(out lpSystemInfo);
                    return;
                }

                _getSystemInfoOriginal(out lpSystemInfo);

                if (_config.Cpu != null && _config.Cpu.CoreCount > 0)
                {
                    lpSystemInfo.dwNumberOfProcessors = (uint)_config.Cpu.CoreCount;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GetSystemInfo Hook 回调执行异常。", ex, "HookManager");

                // 发生异常时尽力返回真实信息，避免目标进程崩溃
                GetSystemInfoNative(out lpSystemInfo);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetSystemInfoNative(out SYSTEM_INFO lpSystemInfo);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        private delegate void GetSystemInfoDelegate(out SYSTEM_INFO lpSystemInfo);

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

        public void Dispose()
        {
            UninstallAll();
        }
    }
}

