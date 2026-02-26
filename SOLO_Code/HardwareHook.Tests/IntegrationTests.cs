using HardwareHook.Core.Configuration;
using HardwareHook.Core.Hooking;
using HardwareHook.Core.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace HardwareHook.Tests
{
    /// <summary>
    /// 集成测试
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private HardwareConfig _testConfig;
        private FileLogger _testLogger;
        private string _testLogDirectory;

        [SetUp]
        public void Setup()
        {
            // 创建测试配置
            _testConfig = new HardwareConfig
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

            // 创建测试日志目录
            _testLogDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HardwareHookIntegrationTests", "logs");
            
            // 清理旧的测试目录
            if (System.IO.Directory.Exists(_testLogDirectory))
                System.IO.Directory.Delete(_testLogDirectory, true);

            // 确保测试目录存在
            System.IO.Directory.CreateDirectory(_testLogDirectory);

            // 创建测试日志记录器
            _testLogger = new FileLogger(_testLogDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // 卸载所有钩子
            HookManager.UninstallAll();

            // 清理测试目录
            if (System.IO.Directory.Exists(_testLogDirectory))
                System.IO.Directory.Delete(_testLogDirectory, true);
        }

        /// <summary>
        /// 测试钩子安装和卸载
        /// </summary>
        [Test]
        public void HookManager_InstallAndUninstallAll_WorksCorrectly()
        {
            // 执行 - 安装钩子
            HookManager.InstallAll(_testConfig, _testLogger);

            // 执行 - 卸载钩子
            HookManager.UninstallAll();

            // 验证 - 没有抛出异常，说明安装和卸载成功
            Assert.Pass("钩子安装和卸载成功");
        }

        /// <summary>
        /// 测试CPU核心数模拟
        /// </summary>
        [Test]
        public void HookManager_CpuCoreCountSimulation_WorksCorrectly()
        {
            // 安装钩子
            HookManager.InstallAll(_testConfig, _testLogger);

            try
            {
                // 调用GetSystemInfo API
                var systemInfo = new HookManager.SYSTEM_INFO();
                HookManager.GetSystemInfo(ref systemInfo);

                // 验证核心数是否被模拟
                Assert.That(systemInfo.dwNumberOfProcessors, Is.EqualTo((uint)_testConfig.Cpu.CoreCount));
            }
            finally
            {
                // 卸载钩子
                HookManager.UninstallAll();
            }
        }

        /// <summary>
        /// 测试Native CPU核心数模拟
        /// </summary>
        [Test]
        public void HookManager_NativeCpuCoreCountSimulation_WorksCorrectly()
        {
            // 安装钩子
            HookManager.InstallAll(_testConfig, _testLogger);

            try
            {
                // 调用GetNativeSystemInfo API
                var systemInfo = new HookManager.SYSTEM_INFO();
                HookManager.GetNativeSystemInfo(ref systemInfo);

                // 验证核心数是否被模拟
                Assert.That(systemInfo.dwNumberOfProcessors, Is.EqualTo((uint)_testConfig.Cpu.CoreCount));
            }
            finally
            {
                // 卸载钩子
                HookManager.UninstallAll();
            }
        }
    }
}
