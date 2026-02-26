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
    /// 系统测试
    /// </summary>
    [TestFixture]
    public class SystemTests
    {
        private string _testConfigPath;
        private string _testLogDirectory;

        [SetUp]
        public void Setup()
        {
            // 创建测试配置文件路径
            _testConfigPath = Path.Combine(Path.GetTempPath(), "hardwarehook_test_config.json");
            
            // 创建测试日志目录
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "HardwareHookSystemTests", "logs");
            
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
        /// 测试完整的硬件信息模拟流程
        /// </summary>
        [Test]
        public void CompleteHardwareSimulationFlow_WorksCorrectly()
        {
            // 1. 创建测试配置
            var testConfig = new HardwareConfig
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

            // 2. 保存配置文件
            var saveResult = ConfigurationLoader.SaveConfiguration(testConfig, _testConfigPath);
            Assert.That(saveResult, Is.True);
            Assert.That(File.Exists(_testConfigPath), Is.True);

            // 3. 加载配置文件
            var loadResult = ConfigurationLoader.LoadConfiguration(_testConfigPath);
            Assert.That(loadResult.Success, Is.True);
            Assert.That(loadResult.Config, Is.Not.Null);
            Assert.That(loadResult.Config.Cpu.CoreCount, Is.EqualTo(testConfig.Cpu.CoreCount));

            // 4. 创建日志记录器
            var logger = new FileLogger(_testLogDirectory);

            // 5. 安装钩子
            HookManager.InstallAll(loadResult.Config, logger);

            try
            {
                // 6. 读取硬件信息
                var hardwareInfo = HardwareInfoReader.ReadHardwareInfo();
                Assert.That(hardwareInfo, Is.Not.Null);
                Assert.That(hardwareInfo.CpuModel, Is.Not.Null);
                Assert.That(hardwareInfo.CpuCoreCount, Is.GreaterThanOrEqualTo(0));
                Assert.That(hardwareInfo.CpuId, Is.Not.Null);
                Assert.That(hardwareInfo.DiskSerial, Is.Not.Null);
                Assert.That(hardwareInfo.MacAddress, Is.Not.Null);
                Assert.That(hardwareInfo.MotherboardSerial, Is.Not.Null);

                // 7. 验证CPU核心数模拟
                var systemInfo = new HookManager.SYSTEM_INFO();
                HookManager.GetSystemInfo(ref systemInfo);
                Assert.That(systemInfo.dwNumberOfProcessors, Is.EqualTo((uint)testConfig.Cpu.CoreCount));
            }
            finally
            {
                // 8. 卸载钩子
                HookManager.UninstallAll();
            }

            // 验证整个流程成功完成
            Assert.Pass("完整硬件信息模拟流程测试成功");
        }

        /// <summary>
        /// 测试默认配置流程
        /// </summary>
        [Test]
        public void DefaultConfigurationFlow_WorksCorrectly()
        {
            // 1. 创建默认配置
            var defaultConfig = new HardwareConfig();

            // 2. 创建日志记录器
            var logger = new FileLogger(_testLogDirectory);

            // 3. 安装钩子
            HookManager.InstallAll(defaultConfig, logger);

            try
            {
                // 4. 读取硬件信息
                var hardwareInfo = HardwareInfoReader.ReadHardwareInfo();
                Assert.That(hardwareInfo, Is.Not.Null);
                Assert.That(hardwareInfo.CpuModel, Is.Not.Null);

                // 5. 验证CPU核心数模拟
                var systemInfo = new HookManager.SYSTEM_INFO();
                HookManager.GetSystemInfo(ref systemInfo);
                Assert.That(systemInfo.dwNumberOfProcessors, Is.EqualTo((uint)defaultConfig.Cpu.CoreCount));
            }
            finally
            {
                // 6. 卸载钩子
                HookManager.UninstallAll();
            }

            // 验证默认配置流程成功完成
            Assert.Pass("默认配置流程测试成功");
        }

        /// <summary>
        /// 测试硬件信息读取和钩子集成
        /// </summary>
        [Test]
        public void HardwareInfoReadingWithHooks_WorksCorrectly()
        {
            // 1. 创建测试配置
            var testConfig = new HardwareConfig
            {
                Version = "1.0",
                Cpu = new CpuConfig
                {
                    Model = "Intel(R) Core(TM) i7-11700K",
                    CoreCount = 8,
                    CpuId = "BFEBFBFF000A06E9"
                }
            };

            // 2. 创建日志记录器
            var logger = new FileLogger(_testLogDirectory);

            // 3. 读取原始硬件信息
            var originalInfo = HardwareInfoReader.ReadHardwareInfo();
            Assert.That(originalInfo, Is.Not.Null);

            // 4. 安装钩子
            HookManager.InstallAll(testConfig, logger);

            try
            {
                // 5. 读取钩子后的硬件信息
                var hookedInfo = HardwareInfoReader.ReadHardwareInfo();
                Assert.That(hookedInfo, Is.Not.Null);

                // 6. 验证系统信息API被钩子
                var systemInfo = new HookManager.SYSTEM_INFO();
                HookManager.GetSystemInfo(ref systemInfo);
                Assert.That(systemInfo.dwNumberOfProcessors, Is.EqualTo((uint)testConfig.Cpu.CoreCount));
            }
            finally
            {
                // 7. 卸载钩子
                HookManager.UninstallAll();
            }

            // 验证硬件信息读取和钩子集成成功
            Assert.Pass("硬件信息读取和钩子集成测试成功");
        }
    }
}
