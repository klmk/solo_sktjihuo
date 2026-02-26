namespace HardwareHook.Core.HardwareInfo
{
    /// <summary>
    /// 硬件信息快照
    /// </summary>
    public class HardwareInfoSnapshot
    {
        /// <summary>
        /// CPU型号
        /// </summary>
        public string CpuModel { get; set; } = "Unknown";

        /// <summary>
        /// CPU核心数
        /// </summary>
        public int CpuCoreCount { get; set; } = 0;

        /// <summary>
        /// CPU ID
        /// </summary>
        public string CpuId { get; set; } = "Unknown";

        /// <summary>
        /// 硬盘序列号
        /// </summary>
        public string DiskSerial { get; set; } = "Unknown";

        /// <summary>
        /// MAC地址
        /// </summary>
        public string MacAddress { get; set; } = "Unknown";

        /// <summary>
        /// 主板序列号
        /// </summary>
        public string MotherboardSerial { get; set; } = "Unknown";

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// 是否成功读取
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error);

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>硬件信息字符串</returns>
        public override string ToString()
        {
            if (!IsSuccess)
            {
                return $"Error: {Error}";
            }

            return $"CPU: {CpuModel} ({CpuCoreCount} cores)\n" +
                   $"CPU ID: {CpuId}\n" +
                   $"Disk Serial: {DiskSerial}\n" +
                   $"MAC Address: {MacAddress}\n" +
                   $"Motherboard Serial: {MotherboardSerial}";
        }
    }
}
