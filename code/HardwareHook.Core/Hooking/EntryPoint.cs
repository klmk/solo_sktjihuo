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
            _uninstallEventName = uninstallEventName ?? string.Empty;
            var baseLogDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "HardwareHook",
                "Logs");

            try
            {
                Directory.CreateDirectory(baseLogDir);
                _logger = new FileLogger(baseLogDir);
            }
            catch (Exception ex)
            {
                TryWriteFallbackLog(baseLogDir, "FileLogger 创建失败: " + ex);
                _logger = new NullLogger();
            }

            try
            {
                _config = LoadConfigSafe(configPath);
                _hookManager = new HookManager(_config, _logger);
            }
            catch (Exception ex)
            {
                _logger.Error("EntryPoint 初始化异常，使用默认配置。", ex, "EntryPoint");
                TryWriteFallbackLog(baseLogDir, "EntryPoint 初始化: " + ex);
                _config = new HardwareConfig();
                _hookManager = new HookManager(_config, _logger);
            }
        }

        private static HardwareConfig LoadConfigSafe(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                return new HardwareConfig();
            var result = ConfigurationLoader.Load(configPath, null);
            return result.Config ?? new HardwareConfig();
        }

        private static void TryWriteFallbackLog(string logDir, string content)
        {
            try
            {
                Directory.CreateDirectory(logDir);
                var path = Path.Combine(logDir, "entrypoint_fail.txt");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + content + Environment.NewLine);
            }
            catch { }
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

