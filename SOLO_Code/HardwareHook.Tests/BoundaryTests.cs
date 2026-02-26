using System.IO;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.Hooking;
using HardwareHook.Core.HardwareInfo;
using HardwareHook.Core.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace HardwareHook.Tests
{
    /// <summary>
    /// 边界测试
    /// </summary>
    [TestFixture]
    public class BoundaryTests
    {
        private string _testConfigPath;
        private string _testLogDirectory;

        [SetUp]
        public void Setup()
        {
            // 创建测试配置文件路径
            _testConfigPath = Path.Combine(Path.GetTempPath(), "hardwarehook_boundary_test_config.json");
            
            // 创建测试日志目录
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "HardwareHookBoundaryTests", "logs");
            
            // 清理旧的测试文件和目录
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (Directory.Exists(_testLogDirectory))
                Directory.Delete(_testLogDirectory, true);

            // 确保测试目录存在
            Directory.CreateDirectory(_testLogDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试文件和目录
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (Directory.Exists(_testLogDirectory))
                Directory.Delete(_testLogDirectory, true);

            // 卸载所有钩子
            HookManager.UninstallAll();
        }

        /// <summary>
        /// 测试空配置文件
        /// </summary>
        [Test]
        public void LoadConfiguration_EmptyFile_ReturnsFailure()
        {
            // 准备 - 创建空配置文件
            File.WriteAllText(_testConfigPath, string.Empty);

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Config, Is.Null);
        }

        /// <summary>
        /// 测试CPU核心数为负数的配置
        /// </summary>
        [Test]
        public void LoadConfiguration_NegativeCoreCount_ReturnsFailure()
        {
            // 准备 - 创建CPU核心数为负数的配置文件
            var invalidConfigJson = @"{
                ""Version"": ""1.0"",
                ""Cpu"": {
                    ""Model"": ""Intel(R) Core(TM) i9-12900K"",
                    ""CoreCount"": -4,
                    ""CpuId"": ""BFEBFBFF000A06E9""
                },
                ""Disk"": {
                    ""Serial"": ""1234567890ABCDEF""
                },
                ""Mac"": {
                    ""Address"": ""00:11:22:33:44:55""
                },
                ""Motherboard"": {
                    ""Serial"": ""MB-2025-12345678""
                }
            }";

            File.WriteAllText(_testConfigPath, invalidConfigJson);

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Config, Is.Null);
        }

        /// <summary>
        /// 测试CPU核心数为0的配置
        /// </summary>
        [Test]
        public void LoadConfiguration_ZeroCoreCount_ReturnsFailure()
        {
            // 准备 - 创建CPU核心数为0的配置文件
            var invalidConfigJson = @"{
                ""Version"": ""1.0"",
                ""Cpu"": {
                    ""Model"": ""Intel(R) Core(TM) i9-12900K"",
                    ""CoreCount"": 0,
                    ""CpuId"": ""BFEBFBFF000A06E9""
                },
                ""Disk"": {
                    ""Serial"": ""1234567890ABCDEF""
                },
                ""Mac"": {
                    ""Address"": ""00:11:22:33:44:55""
                },
                ""Motherboard"": {
                    ""Serial"": ""MB-2025-12345678""
                }
            }";

            File.WriteAllText(_testConfigPath, invalidConfigJson);

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Config, Is.Null);
        }

        /// <summary>
        /// 测试空配置文件路径
        /// </summary>
        [Test]
        public void LoadConfiguration_EmptyPath_ReturnsFailure()
        {
            // 执行
            var result = ConfigurationLoader.LoadConfiguration(string.Empty);

            // 验证
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Config, Is.Null);
        }

        /// <summary>
        /// 测试配置文件路径为null
        /// </summary>
        [Test]
        public void LoadConfiguration_NullPath_ReturnsFailure()
        {
            // 执行
            var result = ConfigurationLoader.LoadConfiguration(null);

            // 验证
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.Config, Is.Null);
        }

        /// <summary>
        /// 测试日志目录不存在
        /// </summary>
        [Test]
        public void FileLogger_NonExistentLogDirectory_CreatesDirectory()
        {
            // 准备 - 确保日志目录不存在
            var nonExistentLogDir = Path.Combine(Path.GetTempPath(), "NonExistentLogDir", "logs");
            if (Directory.Exists(nonExistentLogDir))
                Directory.Delete(nonExistentLogDir, true);

            try
            {
                // 执行
                var logger = new FileLogger(nonExistentLogDir);
                logger.Info("Test log message");

                // 验证
                Assert.That(Directory.Exists(nonExistentLogDir), Is.True);
            }
            finally
            {
                // 清理
                if (Directory.Exists(nonExistentLogDir))
                    Directory.Delete(nonExistentLogDir, true);
            }
        }

        /// <summary>
        /// 测试硬件信息读取失败
        /// </summary>
        [Test]
        public void HardwareInfoReader_ReadHardwareInfo_HandlesFailure()
        {
            // 执行
            var snapshot = HardwareInfoReader.ReadHardwareInfo();

            // 验证
            Assert.That(snapshot, Is.Not.Null);
            // 即使读取失败，也应该返回有效的快照对象，只是字段值可能为"Unknown"
            Assert.That(snapshot.CpuModel, Is.Not.Null);
        }

        /// <summary>
        /// 测试钩子安装时配置为null
        /// </summary>
        [Test]
        public void HookManager_InstallAll_NullConfig_HandlesGracefully()
        {
            // 准备
            var logger = new FileLogger(_testLogDirectory);

            // 执行
            try
            {
                HookManager.InstallAll(null, logger);
                // 验证 - 没有抛出异常，说明安装过程成功处理了null配置
                Assert.Pass("钩子安装时配置为null，处理成功");
            }
            finally
            {
                // 清理
                HookManager.UninstallAll();
            }
        }

        /// <summary>
        /// 测试钩子安装时日志器为null
        /// </summary>
        [Test]
        public void HookManager_InstallAll_NullLogger_HandlesGracefully()
        {
            // 准备
            var config = new HardwareConfig();

            // 执行
            try
            {
                HookManager.InstallAll(config, null);
                // 验证 - 没有抛出异常，说明安装过程成功处理了null日志器
                Assert.Pass("钩子安装时日志器为null，处理成功");
            }
            finally
            {
                // 清理
                HookManager.UninstallAll();
            }
        }

        /// <summary>
        /// 测试保存配置时配置为null
        /// </summary>
        [Test]
        public void SaveConfiguration_NullConfig_ReturnsFailure()
        {
            // 执行
            var result = ConfigurationLoader.SaveConfiguration(null, _testConfigPath);

            // 验证
            Assert.That(result, Is.False);
        }

        /// <summary>
        /// 测试保存配置时路径为null
        /// </summary>
        [Test]
        public void SaveConfiguration_NullPath_ReturnsFailure()
        {
            // 准备
            var config = new HardwareConfig();

            // 执行
            var result = ConfigurationLoader.SaveConfiguration(config, null);

            // 验证
            Assert.That(result, Is.False);
        }
    }
}
