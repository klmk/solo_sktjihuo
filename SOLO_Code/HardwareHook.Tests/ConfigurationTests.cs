using System.IO;
using HardwareHook.Core.Configuration;
using NUnit.Framework;

namespace HardwareHook.Tests
{
    /// <summary>
    /// 配置加载模块测试
    /// </summary>
    [TestFixture]
    public class ConfigurationTests
    {
        private string _testConfigPath;
        private string _nonExistentConfigPath;

        [SetUp]
        public void Setup()
        {
            // 创建测试配置文件路径
            _testConfigPath = Path.Combine(Path.GetTempPath(), "test_config.json");
            _nonExistentConfigPath = Path.Combine(Path.GetTempPath(), "non_existent_config.json");

            // 确保临时文件不存在
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (File.Exists(_nonExistentConfigPath))
                File.Delete(_nonExistentConfigPath);
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试文件
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (File.Exists(_nonExistentConfigPath))
                File.Delete(_nonExistentConfigPath);
        }

        /// <summary>
        /// 测试加载不存在的配置文件
        /// </summary>
        [Test]
        public void LoadConfiguration_NonExistentFile_ReturnsFailure()
        {
            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_nonExistentConfigPath);

            // 验证
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Config);
            Assert.AreEqual("配置文件不存在", result.ErrorMessage);
        }

        /// <summary>
        /// 测试加载格式正确的配置文件
        /// </summary>
        [Test]
        public void LoadConfiguration_ValidConfig_ReturnsSuccess()
        {
            // 准备
            var validConfig = new HardwareConfig
            {
                Version = "1.0",
                Cpu = new CpuConfig
                {
                    Model = "Intel(R) Core(TM) i9-12900K",
                    CoreCount = 16,
                    CpuId = "BFEBFBFF000A06E9"
                },
                Disk = new DiskConfig
                {
                    Serial = "1234567890ABCDEF"
                },
                Mac = new MacConfig
                {
                    Address = "00:11:22:33:44:55"
                },
                Motherboard = new MotherboardConfig
                {
                    Serial = "MB-2025-12345678"
                }
            };

            // 保存配置文件
            ConfigurationLoader.SaveConfiguration(validConfig, _testConfigPath);

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.IsTrue(result.Success);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNotNull(result.Config);
            Assert.AreEqual(validConfig.Version, result.Config.Version);
            Assert.AreEqual(validConfig.Cpu.Model, result.Config.Cpu.Model);
            Assert.AreEqual(validConfig.Cpu.CoreCount, result.Config.Cpu.CoreCount);
            Assert.AreEqual(validConfig.Cpu.CpuId, result.Config.Cpu.CpuId);
            Assert.AreEqual(validConfig.Disk.Serial, result.Config.Disk.Serial);
            Assert.AreEqual(validConfig.Mac.Address, result.Config.Mac.Address);
            Assert.AreEqual(validConfig.Motherboard.Serial, result.Config.Motherboard.Serial);
        }

        /// <summary>
        /// 测试加载格式错误的配置文件
        /// </summary>
        [Test]
        public void LoadConfiguration_InvalidJson_ReturnsFailure()
        {
            // 准备 - 创建格式错误的配置文件
            File.WriteAllText(_testConfigPath, "{invalid json}");

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Config);
            Assert.IsTrue(result.ErrorMessage.Contains("配置加载失败"));
        }

        /// <summary>
        /// 测试加载验证失败的配置文件（核心数为0）
        /// </summary>
        [Test]
        public void LoadConfiguration_InvalidConfig_ReturnsFailure()
        {
            // 准备 - 创建验证失败的配置文件（核心数为0）
            var invalidConfigJson = "{\"Version\": \"1.0\", \"Cpu\": {\"Model\": \"Intel(R) Core(TM) i9-12900K\", \"CoreCount\": 0, \"CpuId\": \"BFEBFBFF000A06E9\"}, \"Disk\": {\"Serial\": \"1234567890ABCDEF\"}, \"Mac\": {\"Address\": \"00:11:22:33:44:55\"}, \"Motherboard\": {\"Serial\": \"MB-2025-12345678\"}}";

            File.WriteAllText(_testConfigPath, invalidConfigJson);

            // 执行
            var result = ConfigurationLoader.LoadConfiguration(_testConfigPath);

            // 验证
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Config);
            Assert.AreEqual("配置文件验证失败", result.ErrorMessage);
        }

        /// <summary>
        /// 测试保存配置文件
        /// </summary>
        [Test]
        public void SaveConfiguration_ValidConfig_ReturnsSuccess()
        {
            // 准备
            var validConfig = new HardwareConfig
            {
                Version = "1.0",
                Cpu = new CpuConfig
                {
                    Model = "Intel(R) Core(TM) i9-12900K",
                    CoreCount = 16,
                    CpuId = "BFEBFBFF000A06E9"
                },
                Disk = new DiskConfig
                {
                    Serial = "1234567890ABCDEF"
                },
                Mac = new MacConfig
                {
                    Address = "00:11:22:33:44:55"
                },
                Motherboard = new MotherboardConfig
                {
                    Serial = "MB-2025-12345678"
                }
            };

            // 执行
            var result = ConfigurationLoader.SaveConfiguration(validConfig, _testConfigPath);

            // 验证
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(_testConfigPath));

            // 验证保存的配置可以正确加载
            var loadResult = ConfigurationLoader.LoadConfiguration(_testConfigPath);
            Assert.IsTrue(loadResult.Success);
            Assert.IsNotNull(loadResult.Config);
            Assert.AreEqual(validConfig.Version, loadResult.Config.Version);
        }
    }
}
