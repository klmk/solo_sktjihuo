using System;
using System.IO;
using System.Text;

namespace HardwareHook.Core.Logging
{
    /// <summary>
    /// 文件日志记录器
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logDirectory">日志目录</param>
        public FileLogger(string logDirectory = null)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            // 确保日志目录存在
            Directory.CreateDirectory(logDirectory);

            // 生成日志文件路径
            string logFileName = $"hardwarehook_{DateTime.Now:yyyyMMdd}.log";
            _logFilePath = Path.Combine(logDirectory, logFileName);
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Debug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Warn(string message)
        {
            WriteLog(LogLevel.Warn, message);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error(string message)
        {
            WriteLog(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常</param>
        public void Error(string message, Exception ex)
        {
            WriteLog(LogLevel.Error, $"{message}\n{ex.ToString()}");
        }

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Fatal(string message)
        {
            WriteLog(LogLevel.Fatal, message);
        }

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常</param>
        public void Fatal(string message, Exception ex)
        {
            WriteLog(LogLevel.Fatal, $"{message}\n{ex.ToString()}");
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        private void WriteLog(LogLevel level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    
                    // 异步写入日志
                    using (StreamWriter writer = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(logEntry);
                    }
                }
            }
            catch
            {
                // 日志写入失败时静默处理
            }
        }
    }
}
