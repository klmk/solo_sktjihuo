using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HardwareHook.Core.Logging
{
    public sealed class FileLogger : ILogger, IDisposable
    {
        private readonly string _logDirectory;
        private readonly string _logFilePrefix;
        private readonly BlockingCollection<LogEntry> _queue;
        private readonly CancellationTokenSource _cts;
        private readonly Task _workerTask;

        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        public FileLogger(string logDirectory, string logFilePrefix = "hardwarehook")
        {
            _logDirectory = logDirectory;
            _logFilePrefix = string.IsNullOrWhiteSpace(logFilePrefix) ? "hardwarehook" : logFilePrefix;
            Directory.CreateDirectory(_logDirectory);

            _queue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>());
            _cts = new CancellationTokenSource();
            _workerTask = Task.Factory.StartNew(
                WorkerLoop,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Log(LogLevel level, string message, Exception? exception = null, string? module = null)
        {
            if (level < MinimumLevel)
            {
                return;
            }

            var entry = new LogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Message = message ?? string.Empty,
                Module = module,
                Exception = exception
            };

            // 防御：队列关闭或满时直接丢弃，避免影响主流程
            if (!_queue.IsAddingCompleted)
            {
                try
                {
                    _queue.Add(entry);
                }
                catch
                {
                    // 忽略日志写入异常，确保不影响业务
                }
            }
        }

        public void Debug(string message, string? module = null) => Log(LogLevel.Debug, message, null, module);

        public void Info(string message, string? module = null) => Log(LogLevel.Info, message, null, module);

        public void Warning(string message, string? module = null) => Log(LogLevel.Warning, message, null, module);

        public void Error(string message, Exception? exception = null, string? module = null) =>
            Log(LogLevel.Error, message, exception, module);

        private void WorkerLoop()
        {
            try
            {
                foreach (var entry in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        WriteEntry(entry);
                    }
                    catch
                    {
                        // 单条写入失败不影响后续
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常结束
            }
        }

        private void WriteEntry(LogEntry entry)
        {
            var filePath = GetLogFilePath(entry.TimestampUtc);

            var payload = new SerializableLogEntry
            {
                timestamp = entry.TimestampUtc.ToString("o", CultureInfo.InvariantCulture),
                level = entry.Level.ToString().ToUpperInvariant(),
                message = entry.Message,
                module = entry.Module,
                error = entry.Exception?.Message,
                exceptionType = entry.Exception?.GetType().FullName,
                stackTrace = entry.Exception?.StackTrace
            };

            var line = JsonConvert.SerializeObject(payload);

            File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
        }

        private string GetLogFilePath(DateTime timestampUtc)
        {
            var local = timestampUtc.ToLocalTime();
            var fileName = $"{_logFilePrefix}_{local:yyyyMMdd}.log";
            return Path.Combine(_logDirectory, fileName);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.CompleteAdding();

            try
            {
                _workerTask.Wait(1000);
            }
            catch
            {
                // 忽略结束异常
            }

            _cts.Dispose();
        }

        private sealed class LogEntry
        {
            public DateTime TimestampUtc { get; set; }

            public LogLevel Level { get; set; }

            public string Message { get; set; } = string.Empty;

            public string? Module { get; set; }

            public Exception? Exception { get; set; }
        }

        private sealed class SerializableLogEntry
        {
            public string? timestamp { get; set; }
            public string? level { get; set; }
            public string? message { get; set; }
            public string? module { get; set; }
            public string? error { get; set; }
            public string? exceptionType { get; set; }
            public string? stackTrace { get; set; }
        }
    }
}

