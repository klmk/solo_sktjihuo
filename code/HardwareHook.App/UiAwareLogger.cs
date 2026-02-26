using System;
using System.Windows.Forms;
using HardwareHook.Core.Logging;

namespace HardwareHook.App
{
    /// <summary>
    /// 将日志同时写入文件和 UI（主窗体日志查看标签页）的 ILogger 包装实现。
    /// </summary>
    internal sealed class UiAwareLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly Control _invoker;
        private readonly Action<string> _appendToUi;

        public LogLevel MinimumLevel
        {
            get => _inner.MinimumLevel;
            set => _inner.MinimumLevel = value;
        }

        public UiAwareLogger(ILogger inner, Control uiInvoker, Action<string> appendToUi)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _invoker = uiInvoker ?? throw new ArgumentNullException(nameof(uiInvoker));
            _appendToUi = appendToUi ?? throw new ArgumentNullException(nameof(appendToUi));
        }

        public void Log(LogLevel level, string message, Exception exception = null, string module = null)
        {
            _inner.Log(level, message, exception, module);

            var line = FormatLine(level, message, exception, module);
            if (_invoker.IsDisposed) return;
            try
            {
                if (!_invoker.IsHandleCreated)
                {
                    SafeAppend(line);
                    return;
                }
                if (_invoker.InvokeRequired)
                    _invoker.BeginInvoke(new Action(() => SafeAppend(line)));
                else
                    SafeAppend(line);
            }
            catch { /* 忽略 UI 更新异常 */ }
        }

        private void SafeAppend(string line)
        {
            try { _appendToUi(line); } catch { }
        }

        private static string FormatLine(LogLevel level, string message, Exception ex, string module)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var levelStr = level.ToString().ToUpperInvariant();
            var prefix = string.IsNullOrEmpty(module) ? $"{time} [{levelStr}]" : $"{time} [{levelStr}] [{module}]";
            var text = $"{prefix} {message}";
            if (ex != null)
                text += Environment.NewLine + "  " + ex.Message;
            return text;
        }

        public void Debug(string message, string module = null) => _inner.Debug(message, module);
        public void Info(string message, string module = null) => _inner.Info(message, module);
        public void Warning(string message, string module = null) => _inner.Warning(message, module);
        public void Error(string message, Exception exception = null, string module = null) => _inner.Error(message, exception, module);
    }
}
