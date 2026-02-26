using System;

namespace HardwareHook.Core.Logging
{
    public interface ILogger
    {
        LogLevel MinimumLevel { get; set; }

        void Log(LogLevel level, string message, Exception? exception = null, string? module = null);

        void Debug(string message, string? module = null);

        void Info(string message, string? module = null);

        void Warning(string message, string? module = null);

        void Error(string message, Exception? exception = null, string? module = null);
    }
}

