namespace HardwareHook.Core.HardwareInfo
{
    public class HardwareInfoSnapshot
    {
        public string CpuModel { get; set; } = string.Empty;

        public int CpuCoreCount { get; set; }

        public string CpuId { get; set; } = string.Empty;

        public string DiskSerial { get; set; } = string.Empty;

        public string MacAddress { get; set; } = string.Empty;

        public string MotherboardSerial { get; set; } = string.Empty;
    }
}

