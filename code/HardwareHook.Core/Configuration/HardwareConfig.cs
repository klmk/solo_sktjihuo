using Newtonsoft.Json;

namespace HardwareHook.Core.Configuration
{
    public class HardwareConfig
    {
        public string Version { get; set; } = "1.0";

        public CpuConfig Cpu { get; set; } = new CpuConfig();

        public DiskConfig Disk { get; set; } = new DiskConfig();

        public MacConfig Mac { get; set; } = new MacConfig();

        public MotherboardConfig Motherboard { get; set; } = new MotherboardConfig();
    }

    public class CpuConfig
    {
        public string Model { get; set; } = string.Empty;

        public int CoreCount { get; set; }

        public string CpuId { get; set; } = string.Empty;
    }

    public class DiskConfig
    {
        public string Serial { get; set; } = string.Empty;
    }

    public class MacConfig
    {
        public string Address { get; set; } = string.Empty;
    }

    public class MotherboardConfig
    {
        public string Serial { get; set; } = string.Empty;
    }
}

