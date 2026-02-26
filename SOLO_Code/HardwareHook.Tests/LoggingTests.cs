using System.IO;
using System.Linq;
using HardwareHook.Core.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace HardwareHook.Tests
{
    /// <summary>
    /// 日志模块测试
    /// </summary>
    [TestFixture]
    public class LoggingTests
    {
        private string _testLogDirectory;
        private string _logFilePath;

        [SetUp]
        public void Setup()
        {
            // 创建测试日志目录
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "HardwareHookTests", "logs");
            
            // 清理旧的测试目录
            if (Directory.Exists(_testLogDirectory))
                Directory.Delete(_testLogDirectory, true);

            // 确保测试目录存在
            Directory.CreateDirectory(_testLogDirectory);

            // 生成日志文件路径
            string logFileName = $"hardwarehook_{System.DateTime.Now:yyyyMMdd}.log";
            _logFilePath = Path.Combine(_testLogDirectory, logFileName);
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试目录
            if (Directory.Exists(_testLogDirectory))
                Directory.Delete(_testLogDirectory, true);
        }

        /// <summary>
        /// 测试FileLogger构造函数
        /// </summary>
        [Test]
        public void FileLogger_Constructor_CreatesLogDirectory()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            
            // 验证
            Assert.That(Directory.Exists(_testLogDirectory), Is.True);
        }

        /// <summary>
        /// 测试记录调试信息
        /// </summary>
        [Test]
        public void FileLogger_Debug_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Debug("Test debug message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Debug] Test debug message"));
        }

        /// <summary>
        /// 测试记录信息
        /// </summary>
        [Test]
        public void FileLogger_Info_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Info("Test info message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Info] Test info message"));
        }

        /// <summary>
        /// 测试记录警告
        /// </summary>
        [Test]
        public void FileLogger_Warn_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Warn("Test warn message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Warn] Test warn message"));
        }

        /// <summary>
        /// 测试记录错误
        /// </summary>
        [Test]
        public void FileLogger_Error_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Error("Test error message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Error] Test error message"));
        }

        /// <summary>
        /// 测试记录带异常的错误
        /// </summary>
        [Test]
        public void FileLogger_ErrorWithException_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            var ex = new System.Exception("Test exception message");
            logger.Error("Test error with exception", ex);
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Error] Test error with exception"));
            Assert.That(logContent, Does.Contain("Test exception message"));
        }

        /// <summary>
        /// 测试记录致命错误
        /// </summary>
        [Test]
        public void FileLogger_Fatal_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Fatal("Test fatal message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Fatal] Test fatal message"));
        }

        /// <summary>
        /// 测试记录带异常的致命错误
        /// </summary>
        [Test]
        public void FileLogger_FatalWithException_WritesToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            var ex = new System.Exception("Test exception message");
            logger.Fatal("Test fatal with exception", ex);
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Fatal] Test fatal with exception"));
            Assert.That(logContent, Does.Contain("Test exception message"));
        }

        /// <summary>
        /// 测试多个日志消息
        /// </summary>
        [Test]
        public void FileLogger_MultipleMessages_WritesAllToLogFile()
        {
            // 执行
            var logger = new FileLogger(_testLogDirectory);
            logger.Debug("Debug message");
            logger.Info("Info message");
            logger.Warn("Warn message");
            logger.Error("Error message");
            logger.Fatal("Fatal message");
            logger.Flush(); // 强制刷新缓冲区

            // 查找实际创建的日志文件
            var logFiles = Directory.GetFiles(_testLogDirectory, "hardwarehook_*.log");
            Assert.That(logFiles.Length, Is.GreaterThan(0));
            _logFilePath = logFiles[0];

            // 验证
            Assert.That(File.Exists(_logFilePath), Is.True);
            string logContent = File.ReadAllText(_logFilePath);
            Assert.That(logContent, Does.Contain("[Debug] Debug message"));
            Assert.That(logContent, Does.Contain("[Info] Info message"));
            Assert.That(logContent, Does.Contain("[Warn] Warn message"));
            Assert.That(logContent, Does.Contain("[Error] Error message"));
            Assert.That(logContent, Does.Contain("[Fatal] Fatal message"));
        }
    }
}
