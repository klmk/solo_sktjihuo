namespace HardwareHook.Core.Configuration
{
    /// <summary>
    /// 配置加载结果
    /// </summary>
    public class ConfigurationLoadResult
    {
        /// <summary>
        /// 是否加载成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 硬件配置
        /// </summary>
        public HardwareConfig Config { get; set; }
    }
}
