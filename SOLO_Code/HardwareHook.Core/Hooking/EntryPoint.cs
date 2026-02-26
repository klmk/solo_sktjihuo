using System;
using System.IO;
using EasyHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Hooking
{
    /// <summary>
    /// Hook入口点
    /// </summary>
    public class EntryPoint : IEntryPoint
    {
        private string _uninstallEventName;
        private string _configPath;
        private ILogger _logger;
        private HardwareConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inContext">注入上下文</param>
        /// <param name="inArg1">参数1：卸载事件名称</param>
        /// <param name="inArg2">参数2：配置文件路径</param>
        public EntryPoint(RemoteHooking.IContext inContext, string inArg1, string inArg2)
        {
            _uninstallEventName = inArg1;
            _configPath = inArg2;
        }

        /// <summary>
        /// 运行Hook
        /// </summary>
        /// <param name="inContext">注入上下文</param>
        /// <param name="inArg1">参数1：卸载事件名称</param>
        /// <param name="inArg2">参数2：配置文件路径</param>
        public void Run(RemoteHooking.IContext inContext, string inArg1, string inArg2)
        {
            try
            {
                // 初始化日志
                _logger = new FileLogger();
                _logger.Info("Hook initialized");

                // 加载配置
                var loadResult = ConfigurationLoader.LoadConfiguration(_configPath);
                if (!loadResult.Success)
                {
                    _logger.Error("Failed to load configuration: " + loadResult.ErrorMessage);
                    return;
                }

                _config = loadResult.Config;
                _logger.Info("Configuration loaded successfully");

                // 安装Hook
                HookManager.InstallAll(_config, _logger);
                _logger.Info("All hooks installed successfully");

                // 等待卸载信号
                _logger.Info("Waiting for uninstall signal...");
                using (var uninstallEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, _uninstallEventName))
                {
                    uninstallEvent.WaitOne();
                }

                // 卸载Hook
                HookManager.UninstallAll();
                _logger.Info("All hooks uninstalled successfully");
            }
            catch (Exception ex)
            {
                _logger?.Error("Hook failed", ex);
            }
            finally
            {
                _logger?.Info("Hook exited");
            }
        }
    }
}
