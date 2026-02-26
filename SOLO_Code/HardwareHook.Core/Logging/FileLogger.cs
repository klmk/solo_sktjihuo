using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HardwareHook.Core.Logging
{
    /// <summary>
    /// 文件日志记录器
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _logDirectory;
        private string _currentLogFilePath;
        private readonly object _lockObject = new object();
        private readonly Queue<string> _logBuffer = new Queue<string>();
        private readonly int _maxBufferSize = 100;
        private readonly long _maxLogFileSize = 10 * 1024 * 1024; // 10MB
        private bool _isFlushing;
        private bool _disposed;

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

            _logDirectory = logDirectory;
            // 确保日志目录存在
            Directory.CreateDirectory(_logDirectory);

            // 初始化日志文件路径
            UpdateLogFilePath();

            // 清理过期日志文件（保留最近7天）
            CleanupOldLogFiles();
        }

        /// <summary>
        /// 更新日志文件路径
        /// </summary>
        private void UpdateLogFilePath()
        {
            string logFileName = $"hardwarehook_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            _currentLogFilePath = Path.Combine(_logDirectory, logFileName);
        }

        /// <summary>
        /// 清理过期日志文件
        /// </summary>
        private void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "hardwarehook_*.log");
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-7))
                    {
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                    }
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        /// <summary>
        /// 检查日志文件大小
        /// </summary>
        private void CheckLogFileSize()
        {
            try
            {
                if (File.Exists(_currentLogFilePath))
                {
                    var fileInfo = new FileInfo(_currentLogFilePath);
                    if (fileInfo.Length > _maxLogFileSize)
                    {
                        UpdateLogFilePath();
                    }
                }
            }
            catch
            {
                // 忽略检查失败
            }
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Debug(string message)
        {
            EnqueueLog(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常</param>
        public void Debug(string message, Exception ex)
        {
            EnqueueLog(LogLevel.Debug, $"{message}\n{ex.ToString()}");
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info(string message)
        {
            EnqueueLog(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Warn(string message)
        {
            EnqueueLog(LogLevel.Warn, message);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error(string message)
        {
            EnqueueLog(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常</param>
        public void Error(string message, Exception ex)
        {
            EnqueueLog(LogLevel.Error, $"{message}\n{ex.ToString()}");
        }

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Fatal(string message)
        {
            EnqueueLog(LogLevel.Fatal, message);
        }

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常</param>
        public void Fatal(string message, Exception ex)
        {
            EnqueueLog(LogLevel.Fatal, $"{message}\n{ex.ToString()}");
        }

        /// <summary>
        /// 将日志加入队列
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        private void EnqueueLog(LogLevel level, string message)
        {
            lock (_lockObject)
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                _logBuffer.Enqueue(logEntry);

                // 当缓冲区达到阈值时刷新
                if (_logBuffer.Count >= _maxBufferSize && !_isFlushing)
                {
                    _isFlushing = true;
                    Task.Run(() => FlushBuffer());
                }
            }
        }

        /// <summary>
        /// 刷新日志缓冲区
        /// </summary>
        private async Task FlushBuffer()
        {
            List<string> logsToWrite = new List<string>();

            // 取出缓冲区中的日志
            lock (_lockObject)
            {
                while (_logBuffer.Count > 0)
                {
                    logsToWrite.Add(_logBuffer.Dequeue());
                }
                _isFlushing = false;
            }

            if (logsToWrite.Count > 0)
            {
                await WriteLogsToFile(logsToWrite);
            }
        }

        /// <summary>
        /// 将日志写入文件
        /// </summary>
        /// <param name="logs">日志条目列表</param>
        /// <returns>任务</returns>
        private async Task WriteLogsToFile(List<string> logs)
        {
            try
            {
                // 检查日志文件大小
                CheckLogFileSize();

                // 写入日志
                using (StreamWriter writer = new StreamWriter(_currentLogFilePath, true, Encoding.UTF8))
                {
                    foreach (var log in logs)
                    {
                        await writer.WriteLineAsync(log);
                    }
                }
            }
            catch
            {
                // 日志写入失败时静默处理
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public void Flush()
        {
            if (_logBuffer.Count > 0)
            {
                Task.Run(() => FlushBuffer()).Wait();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 刷新缓冲区
                    Flush();
                }

                _disposed = true;
            }
        }
    }
}
