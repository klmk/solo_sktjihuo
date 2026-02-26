using System;
using System.IO;
using System.Threading;
using EasyHook;
using HardwareHook.Core.Config;
using HardwareHook.Core.Hook;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core
{
    /// <summary>
    /// DLL入口点
    /// </summary>
    public class EntryPoint : IEntryPoint
    {
        private string _configPath;
        private string _unloadEventName;
        private EventWaitHandle _unloadEvent;
        private int _processId;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EntryPoint(RemoteHooking.IContext context, string channelName, string configPath, string unloadEventName)
        {
            _configPath = configPath;
            _unloadEventName = unloadEventName;
            _processId = RemoteHooking.GetCurrentProcessId();
            
            // 初始化日志系统
            string logDir = Path.Combine(Path.GetDirectoryName(configPath) ?? AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Logger.Initialize(logDir);
            
            Logger.Info($"EntryPoint构造函数调用: PID={_processId}, Config={Path.GetFileName(configPath)}", "EntryPoint");
        }
        
        /// <summary>
        /// 主运行方法
        /// </summary>
        public void Run(RemoteHooking.IContext context, string channelName, string configPath, string unloadEventName)
        {
            try
            {
                Logger.Info("EntryPoint.Run开始执行", "EntryPoint");
                
                // 加载配置
                HardwareConfig config;
                try
                {
                    config = ConfigManager.Load(configPath);
                    Logger.Info("配置加载成功", "EntryPoint");
                }
                catch (Exception ex)
                {
                    Logger.Error($"配置加载失败: {ex.Message}", "EntryPoint");
                    return;
                }
                
                // 安装Hook
                try
                {
                    HookManager.InstallAll(config);
                    Logger.Info("Hook安装完成", "EntryPoint");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Hook安装失败: {ex.Message}", "EntryPoint");
                    return;
                }
                
                // 创建卸载事件
                try
                {
                    _unloadEvent = new EventWaitHandle(false, EventResetMode.ManualReset, unloadEventName);
                    Logger.Info($"卸载事件已创建: {unloadEventName}", "EntryPoint");
                }
                catch (Exception ex)
                {
                    Logger.Error($"卸载事件创建失败: {ex.Message}", "EntryPoint");
                    return;
                }
                
                // 等待卸载信号
                Logger.Info("等待卸载信号...", "EntryPoint");
                _unloadEvent.WaitOne();
                
                Logger.Info("收到卸载信号，开始清理", "EntryPoint");
            }
            catch (Exception ex)
            {
                Logger.Error($"EntryPoint运行异常: {ex.Message}\n{ex.StackTrace}", "EntryPoint");
            }
            finally
            {
                // 卸载Hook
                try
                {
                    HookManager.UninstallAll();
                    Logger.Info("Hook已卸载", "EntryPoint");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Hook卸载失败: {ex.Message}", "EntryPoint");
                }
                
                // 释放资源
                _unloadEvent?.Dispose();
                Logger.Info("EntryPoint.Run结束", "EntryPoint");
            }
        }
    }
}
