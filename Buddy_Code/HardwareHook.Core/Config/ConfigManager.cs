using System;
using System.IO;
using System.Text;
using HardwareHook.Core.Logging;
using Newtonsoft.Json;

namespace HardwareHook.Core.Config
{
    /// <summary>
    /// 配置异常类
    /// </summary>
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// 配置管理类
    /// </summary>
    public static class ConfigManager
    {
        private static readonly object _lock = new object();
        private static HardwareConfig _config;
        private static string _configPath;
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static HardwareConfig Load(string path)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new FileNotFoundException("配置文件不存在", path);
                
                try
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var config = JsonConvert.DeserializeObject<HardwareConfig>(json);
                    Validate(config);
                    
                    _config = config;
                    _configPath = path;
                    
                    Logger.Info($"配置文件加载成功: {Path.GetFileName(path)}", "ConfigManager");
                    return config;
                }
                catch (Exception ex)
                {
                    Logger.Error($"加载配置文件失败: {ex.Message}", "ConfigManager");
                    throw new ConfigurationException("配置文件格式错误", ex);
                }
            }
        }
        
        /// <summary>
        /// 验证配置有效性
        /// </summary>
        public static void Validate(HardwareConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            // 验证CPU配置
            if (config.Cpu != null)
            {
                if (config.Cpu.CoreCount < 1)
                    config.Cpu.CoreCount = 1;
                if (config.Cpu.CoreCount > 128)
                    config.Cpu.CoreCount = 128;
            }
            
            // 验证MAC地址格式
            if (config.Mac?.Address != null)
            {
                // 基本格式验证
                var mac = config.Mac.Address.Replace(":", "").Replace("-", "");
                if (mac.Length != 12)
                {
                    throw new ConfigurationException("MAC地址格式错误，应为12位十六进制数");
                }
            }
            
            Logger.Debug("配置验证通过", "ConfigManager");
        }
        
        /// <summary>
        /// 获取当前配置
        /// </summary>
        public static HardwareConfig GetCurrent()
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }
}
