using System;

namespace HardwareHook.Main
{
    /// <summary>
    /// 进程信息类
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// 进程ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; }
        
        /// <summary>
        /// 进程状态
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// 是否已注入
        /// </summary>
        public bool IsInjected { get; set; }
        
        /// <summary>
        /// 窗口标题（如果有）
        /// </summary>
        public string WindowTitle { get; set; }
        
        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get; set; }
        
        public ProcessInfo(int id, string processName, string status, string windowTitle, DateTime startTime)
        {
            Id = id;
            ProcessName = processName;
            Status = status;
            WindowTitle = windowTitle;
            StartTime = startTime;
            IsInjected = false;
        }
    }
}
