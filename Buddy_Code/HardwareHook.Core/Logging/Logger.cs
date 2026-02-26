using System;
using System.Diagnostics;
using System.IO;

namespace HardwareHook.Core.Logging
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
    
    /// <summary>
    /// 日志记录器
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFile;
        private static LogLevel _minLevel = LogLevel.Info;
        
        /// <summary>
        /// 初始化日志系统
        /// </summary>
        public static void Initialize(string logDirectory)
        {
            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);
                    
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _logFile = Path.Combine(logDirectory, $"HardwareHook_{timestamp}.log");
                    
                    Info("日志系统初始化成功", "Logger");
                }
                catch (Exception ex)
                {
                    Debug($"日志系统初始化失败: {ex.Message}", "Logger");
                }
            }
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        private static void WriteLog(LogLevel level, string message, string module)
        {
            if (level < _minLevel)
                return;
                
            lock (_lock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string levelStr = level.ToString().ToUpper().PadRight(7);
                    string logEntry = $"[{timestamp}] [{levelStr}] [{module}] {message}";
                    
                    // 写入文件
                    if (!string.IsNullOrEmpty(_logFile))
                    {
                        File.AppendAllText(_logFile, logEntry + Environment.NewLine);
                    }
                    
                    // Debug模式输出到调试器
                    if (level == LogLevel.Debug)
                    {
                        Debug.WriteLine(logEntry);
                    }
                }
                catch
                {
                    // 日志写入失败时忽略，避免影响主程序
                }
            }
        }
        
        public static void Debug(string message, string module = "")
        {
            WriteLog(LogLevel.Debug, message, module);
        }
        
        public static void Info(string message, string module = "")
        {
            WriteLog(LogLevel.Info, message, module);
        }
        
        public static void Warning(string message, string module = "")
        {
            WriteLog(LogLevel.Warning, message, module);
        }
        
        public static void Error(string message, string module = "")
        {
            WriteLog(LogLevel.Error, message, module);
        }
    }
}
