using HardwareHook.Core.HardwareInfo;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace HardwareHook.Tests
{
    /// <summary>
    /// 硬件信息读取模块测试
    /// </summary>
    [TestFixture]
    public class HardwareInfoTests
    {
        /// <summary>
        /// 测试读取硬件信息
        /// </summary>
        [Test]
        public void ReadHardwareInfo_ReturnsValidSnapshot()
        {
            // 执行
            var snapshot = HardwareInfoReader.ReadHardwareInfo();

            // 验证
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.CpuModel, Is.Not.Null);
            Assert.That(snapshot.CpuCoreCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(snapshot.CpuId, Is.Not.Null);
            Assert.That(snapshot.DiskSerial, Is.Not.Null);
            Assert.That(snapshot.MacAddress, Is.Not.Null);
            Assert.That(snapshot.MotherboardSerial, Is.Not.Null);
        }

        /// <summary>
        /// 测试硬件信息快照的IsSuccess属性
        /// </summary>
        [Test]
        public void HardwareInfoSnapshot_IsSuccess_ReturnsCorrectValue()
        {
            // 测试无错误情况
            var snapshot1 = new HardwareInfoSnapshot
            {
                Error = null
            };
            Assert.That(snapshot1.IsSuccess, Is.True);

            // 测试空错误字符串情况
            var snapshot2 = new HardwareInfoSnapshot
            {
                Error = string.Empty
            };
            Assert.That(snapshot2.IsSuccess, Is.True);

            // 测试有错误情况
            var snapshot3 = new HardwareInfoSnapshot
            {
                Error = "Test error"
            };
            Assert.That(snapshot3.IsSuccess, Is.False);
        }

        /// <summary>
        /// 测试硬件信息快照的ToString方法
        /// </summary>
        [Test]
        public void HardwareInfoSnapshot_ToString_ReturnsCorrectString()
        {
            // 测试有错误情况
            var errorSnapshot = new HardwareInfoSnapshot
            {
                Error = "Test error"
            };
            Assert.That(errorSnapshot.ToString(), Does.Contain("Error: Test error"));

            // 测试无错误情况
            var successSnapshot = new HardwareInfoSnapshot
            {
                CpuModel = "Test CPU",
                CpuCoreCount = 4,
                CpuId = "Test CPU ID",
                DiskSerial = "Test Disk Serial",
                MacAddress = "00:11:22:33:44:55",
                MotherboardSerial = "Test Motherboard Serial",
                Error = null
            };
            var result = successSnapshot.ToString();
            Assert.That(result, Does.Contain("CPU: Test CPU (4 cores)"));
            Assert.That(result, Does.Contain("CPU ID: Test CPU ID"));
            Assert.That(result, Does.Contain("Disk Serial: Test Disk Serial"));
            Assert.That(result, Does.Contain("MAC Address: 00:11:22:33:44:55"));
            Assert.That(result, Does.Contain("Motherboard Serial: Test Motherboard Serial"));
        }

        /// <summary>
        /// 测试硬件信息快照的默认值
        /// </summary>
        [Test]
        public void HardwareInfoSnapshot_DefaultValues_AreCorrect()
        {
            // 执行
            var snapshot = new HardwareInfoSnapshot();

            // 验证
            Assert.That(snapshot.CpuModel, Is.EqualTo("Unknown"));
            Assert.That(snapshot.CpuCoreCount, Is.EqualTo(0));
            Assert.That(snapshot.CpuId, Is.EqualTo("Unknown"));
            Assert.That(snapshot.DiskSerial, Is.EqualTo("Unknown"));
            Assert.That(snapshot.MacAddress, Is.EqualTo("Unknown"));
            Assert.That(snapshot.MotherboardSerial, Is.EqualTo("Unknown"));
            Assert.That(snapshot.Error, Is.Null);
            Assert.That(snapshot.IsSuccess, Is.True);
        }
    }
}
