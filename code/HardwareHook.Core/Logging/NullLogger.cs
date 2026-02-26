namespace HardwareHook.Core.Logging
{
    /// <summary>
    /// 空实现，用于 EntryPoint 初始化时 FileLogger 创建失败时的降级。
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        public void Log(LogLevel level, string message, System.Exception? exception = null, string? module = null) { }

        public void Debug(string message, string? module = null) { }

        public void Info(string message, string? module = null) { }

        public void Warning(string message, string? module = null) { }

        public void Error(string message, System.Exception? exception = null, string? module = null) { }
    }
}
