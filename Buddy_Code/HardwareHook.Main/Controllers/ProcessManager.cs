using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HardwareHook.Core.Logging;

namespace HardwareHook.Main
{
    /// <summary>
    /// 进程管理器
    /// </summary>
    public class ProcessManager
    {
        private readonly List<ProcessInfo> _processes = new List<ProcessInfo>();
        private readonly object _lock = new object();
        
        /// <summary>
        /// 进程列表更新事件
        /// </summary>
        public event EventHandler ProcessListUpdated;
        
        /// <summary>
        /// 获取进程列表
        /// </summary>
        public IReadOnlyList<ProcessInfo> GetProcesses()
        {
            lock (_lock)
            {
                return _processes.ToList();
            }
        }
        
        /// <summary>
        /// 刷新进程列表
        /// </summary>
        public void RefreshProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => {
                        try
                        {
                            // 过滤掉系统进程和无效进程
                            var name = p.ProcessName;
                            var id = p.Id;
                            return id > 0 && !string.IsNullOrEmpty(name) && 
                                   !name.Equals("Idle", StringComparison.OrdinalIgnoreCase) &&
                                   !name.Equals("System", StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .Select(p => {
                        try
                        {
                            string title = string.Empty;
                            try
                            {
                                title = p.MainWindowTitle ?? string.Empty;
                            }
                            catch { }
                            
                            return new ProcessInfo(
                                p.Id,
                                p.ProcessName,
                                p.Responding ? "运行中" : "无响应",
                                title,
                                p.StartTime
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"获取进程信息失败 {p.Id}: {ex.Message}", "ProcessManager");
                            return null;
                        }
                    })
                    .Where(p => p != null)
                    .OrderBy(p => p.ProcessName)
                    .ToList();
                
                lock (_lock)
                {
                    _processes.Clear();
                    _processes.AddRange(processes);
                }
                
                Logger.Info($"进程列表刷新完成，共 {processes.Count} 个进程", "ProcessManager");
                
                // 触发更新事件
                ProcessListUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error($"刷新进程列表失败: {ex.Message}", "ProcessManager");
                throw;
            }
        }
        
        /// <summary>
        /// 获取指定进程
        /// </summary>
        public ProcessInfo GetProcess(int processId)
        {
            lock (_lock)
            {
                return _processes.FirstOrDefault(p => p.Id == processId);
            }
        }
        
        /// <summary>
        /// 检查进程是否存活
        /// </summary>
        public bool IsProcessAlive(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}
