using System;
using System.IO;
using System.Threading;
using EasyHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Hooking
{
    // EasyHook 注入入口类：被注入到目标进程后，由 EasyHook 调用。
    public class EntryPoint : IEntryPoint
    {
        private readonly ILogger _logger;
        private readonly HardwareConfig _config;
        private readonly HookManager _hookManager;
        private readonly string _uninstallEventName;

        public EntryPoint(RemoteHooking.IContext context, string configPath, string uninstallEventName)
        {
            var baseLogDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "HardwareHook",
                "Logs");

            _logger = new FileLogger(baseLogDir);

            var loadResult = ConfigurationLoader.Load(configPath, _logger);
            _config = loadResult.Config ?? new HardwareConfig();

            if (!loadResult.Success)
            {
                _logger.Error(
                    $"配置文件加载或验证失败，将使用默认配置继续运行。错误：{loadResult.ErrorMessage}",
                    module: "EntryPoint");
            }

            _hookManager = new HookManager(_config, _logger);
            _uninstallEventName = uninstallEventName;
        }

        public void Run(RemoteHooking.IContext context, string configPath, string uninstallEventName)
        {
            _logger.Info("EntryPoint.Run 开始执行，准备安装 Hook。", "EntryPoint");

            try
            {
                _hookManager.InstallAll();

                _logger.Info($"等待卸载事件：{_uninstallEventName}", "EntryPoint");

                try
                {
                    using (var waitHandle = new EventWaitHandle(
                               false,
                               EventResetMode.AutoReset,
                               _uninstallEventName))
                    {
                        waitHandle.WaitOne();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("等待卸载事件时发生异常。", ex, "EntryPoint");
                }
            }
            finally
            {
                _hookManager.UninstallAll();
                _logger.Info("EntryPoint.Run 结束，Hook 已卸载。", "EntryPoint");
            }
        }
    }
}

