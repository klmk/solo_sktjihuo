using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyHook;
using HardwareHook.Core.Config;
using HardwareHook.Core.Logging;

namespace HardwareHook.Main
{
    /// <summary>
    /// 注入状态事件参数
    /// </summary>
    public class InjectionStatusEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public int ProcessId { get; set; }
        public string Message { get; set; }
    }
    
    /// <summary>
    /// 注入结果类
    /// </summary>
    public class InjectionResult
    {
        public bool Success { get; }
        public string Message { get; }
        public string UnloadEventName { get; }
        
        private InjectionResult(bool success, string message, string unloadEventName = null)
        {
            Success = success;
            Message = message;
            UnloadEventName = unloadEventName;
        }
        
        public static InjectionResult SuccessResult(string unloadEventName)
        {
            return new InjectionResult(true, "注入成功", unloadEventName);
        }
        
        public static InjectionResult FailureResult(string message)
        {
            return new InjectionResult(false, message);
        }
    }
    
    /// <summary>
    /// 注入控制器
    /// </summary>
    public class InjectionController
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, string> _injectedProcesses = new Dictionary<int, string>();
        
        /// <summary>
        /// 注入状态变化事件
        /// </summary>
        public event EventHandler<InjectionStatusEventArgs> InjectionStatusChanged;
        
        /// <summary>
        /// 注入目标进程
        /// </summary>
        public InjectionResult Inject(int processId, string configPath)
        {
            lock (_lock)
            {
                try
                {
                    // 验证参数
                    if (processId <= 0)
                        return InjectionResult.FailureResult("无效的进程ID");
                    
                    if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                        return InjectionResult.FailureResult("配置文件不存在");
                    
                    // 检查是否已注入
                    if (_injectedProcesses.ContainsKey(processId))
                        return InjectionResult.FailureResult("该进程已经注入");
                    
                    // 验证配置
                    try
                    {
                        ConfigManager.Load(configPath);
                    }
                    catch (Exception ex)
                    {
                        return InjectionResult.FailureResult($"配置文件无效: {ex.Message}");
                    }
                    
                    // 解析核心DLL路径
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HardwareHook.Core.dll");
                    if (!File.Exists(dllPath))
                    {
                        // 尝试其他可能的路径
                        string[] possiblePaths = {
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "HardwareHook.Core", "bin", "Debug", "HardwareHook.Core.dll"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "HardwareHook.Core", "bin", "Release", "HardwareHook.Core.dll")
                        };
                        
                        dllPath = possiblePaths.FirstOrDefault(File.Exists);
                        
                        if (string.IsNullOrEmpty(dllPath))
                            return InjectionResult.FailureResult("找不到 Hook 核心 DLL");
                    }
                    
                    // 生成卸载事件名称
                    string unloadEventName = $"HardwareHook_Unload_{processId}_{Guid.NewGuid()}";
                    
                    // 执行注入
                    try
                    {
                        RemoteHooking.Inject(
                            processId,
                            InjectionOptions.Default,
                            dllPath,
                            dllPath,
                            configPath,
                            unloadEventName
                        );
                        
                        // 记录已注入进程
                        _injectedProcesses[processId] = unloadEventName;
                        
                        string successMessage = $"进程 {processId} 注入成功";
                        Logger.Info(successMessage, "InjectionController");
                        
                        // 触发成功事件
                        InjectionStatusChanged?.Invoke(this, new InjectionStatusEventArgs
                        {
                            Success = true,
                            ProcessId = processId,
                            Message = successMessage
                        });
                        
                        return InjectionResult.SuccessResult(unloadEventName);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = GetDetailedErrorMessage(ex);
                        Logger.Error($"注入失败: {errorMessage}", "InjectionController");
                        
                        // 触发失败事件
                        InjectionStatusChanged?.Invoke(this, new InjectionStatusEventArgs
                        {
                            Success = false,
                            ProcessId = processId,
                            Message = errorMessage
                        });
                        
                        return InjectionResult.FailureResult(errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"注入异常: {ex.Message}", "InjectionController");
                    return InjectionResult.FailureResult($"注入异常: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 停止模拟
        /// </summary>
        public void StopSimulation(int processId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_injectedProcesses.ContainsKey(processId))
                    {
                        Logger.Warning($"进程 {processId} 未在注入列表中", "InjectionController");
                        return;
                    }
                    
                    string unloadEventName = _injectedProcesses[processId];
                    
                    try
                    {
                        using (var evt = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, unloadEventName))
                        {
                            evt.Set();
                        }
                        
                        _injectedProcesses.Remove(processId);
                        Logger.Info($"进程 {processId} 模拟已停止", "InjectionController");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"停止模拟失败: {ex.Message}", "InjectionController");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"停止模拟异常: {ex.Message}", "InjectionController");
                }
            }
        }
        
        /// <summary>
        /// 停止所有模拟
        /// </summary>
        public void StopAllSimulations()
        {
            lock (_lock)
            {
                var processIds = _injectedProcesses.Keys.ToList();
                foreach (var processId in processIds)
                {
                    try
                    {
                        StopSimulation(processId);
                    }
                    catch { }
                }
            }
        }
        
        /// <summary>
        /// 检查进程是否已注入
        /// </summary>
        public bool IsInjected(int processId)
        {
            lock (_lock)
            {
                return _injectedProcesses.ContainsKey(processId);
            }
        }
        
        /// <summary>
        /// 获取详细错误信息
        /// </summary>
        private string GetDetailedErrorMessage(Exception ex)
        {
            string message = ex.Message;
            
            if (message.Contains("拒绝访问") || message.Contains("Access is denied"))
            {
                return "权限不足，请以管理员身份运行程序";
            }
            else if (message.Contains("无法打开进程") || message.Contains("Unable to open process"))
            {
                return "无法打开目标进程，可能是系统进程或权限不足";
            }
            else if (message.Contains("32位") || message.Contains("64位") || 
                     message.Contains("32-bit") || message.Contains("64-bit"))
            {
                return "进程位数不匹配，请确保目标进程与Hook DLL位数一致";
            }
            else if (message.Contains("进程不存在") || message.Contains("process does not exist"))
            {
                return "目标进程不存在或已退出";
            }
            else
            {
                return $"注入失败: {message}";
            }
        }
    }
}
