using System;

namespace HardwareHook.Core.Configuration
{
    /// <summary>
    /// 硬件配置模型
    /// </summary>
    public class HardwareConfig
    {
        /// <summary>
        /// 配置版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// CPU配置
        /// </summary>
        public CpuConfig Cpu { get; set; } = new CpuConfig();

        /// <summary>
        /// 硬盘配置
        /// </summary>
        public DiskConfig Disk { get; set; } = new DiskConfig();

        /// <summary>
        /// MAC地址配置
        /// </summary>
        public MacConfig Mac { get; set; } = new MacConfig();

        /// <summary>
        /// 主板配置
        /// </summary>
        public MotherboardConfig Motherboard { get; set; } = new MotherboardConfig();
    }

    /// <summary>
    /// CPU配置
    /// </summary>
    public class CpuConfig
    {
        /// <summary>
        /// CPU型号
        /// </summary>
        public string Model { get; set; } = "Intel(R) Core(TM) i9-12900K";

        /// <summary>
        /// 核心数
        /// </summary>
        public int CoreCount { get; set; } = 16;

        /// <summary>
        /// CPU ID
        /// </summary>
        public string CpuId { get; set; } = "BFEBFBFF000A06E9";
    }

    /// <summary>
    /// 硬盘配置
    /// </summary>
    public class DiskConfig
    {
        /// <summary>
        /// 硬盘序列号
        /// </summary>
        public string Serial { get; set; } = "1234567890ABCDEF";
    }

    /// <summary>
    /// MAC地址配置
    /// </summary>
    public class MacConfig
    {
        /// <summary>
        /// MAC地址
        /// </summary>
        public string Address { get; set; } = "00:11:22:33:44:55";
    }

    /// <summary>
    /// 主板配置
    /// </summary>
    public class MotherboardConfig
    {
        /// <summary>
        /// 主板序列号
        /// </summary>
        public string Serial { get; set; } = "MB-2025-12345678";
    }
}
