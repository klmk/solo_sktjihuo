using System;
using System.IO;
using Newtonsoft.Json;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Configuration
{
    /// <summary>
    /// 配置加载器
    /// </summary>
    public class ConfigurationLoader
    {
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <param name="isEncrypted">是否为加密配置文件</param>
        /// <returns>配置加载结果</returns>
        public static ConfigurationLoadResult LoadConfiguration(string configPath, bool isEncrypted = false)
        {
            try
            {
                // 检查配置文件是否存在
                if (!File.Exists(configPath))
                {
                    return new ConfigurationLoadResult
                    {
                        Success = false,
                        ErrorMessage = "配置文件不存在",
                        Config = null
                    };
                }

                // 读取配置文件内容
                string configContent = File.ReadAllText(configPath);

                // 如果是加密配置文件，进行解密
                if (isEncrypted)
                {
                    try
                    {
                        configContent = EncryptionHelper.Decrypt(configContent);
                    }
                    catch
                    {
                        return new ConfigurationLoadResult
                        {
                            Success = false,
                            ErrorMessage = "配置文件解密失败",
                            Config = null
                        };
                    }
                }

                // 解析配置文件
                var config = JsonConvert.DeserializeObject<HardwareConfig>(configContent);

                // 验证并修复配置
                if (!ValidateAndFixConfiguration(config))
                {
                    return new ConfigurationLoadResult
                    {
                        Success = false,
                        ErrorMessage = "配置文件验证失败",
                        Config = null
                    };
                }

                return new ConfigurationLoadResult
                {
                    Success = true,
                    ErrorMessage = null,
                    Config = config
                };
            }
            catch (Exception ex)
            {
                return new ConfigurationLoadResult
                {
                    Success = false,
                    ErrorMessage = "配置加载失败: " + ex.Message,
                    Config = null
                };
            }
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <param name="configPath">配置文件路径</param>
        /// <param name="isEncrypted">是否加密保存</param>
        /// <returns>保存结果</returns>
        public static bool SaveConfiguration(HardwareConfig config, string configPath, bool isEncrypted = false)
        {
            try
            {
                // 检查配置是否为null
                if (config == null)
                    return false;

                // 检查配置路径是否为null
                if (string.IsNullOrEmpty(configPath))
                    return false;

                // 序列化配置
                string configContent = JsonConvert.SerializeObject(config, Formatting.Indented);

                // 如果需要加密，进行加密
                if (isEncrypted)
                {
                    configContent = EncryptionHelper.Encrypt(configContent);
                }

                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                // 保存配置文件
                File.WriteAllText(configPath, configContent);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证并修复配置
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <returns>验证结果</returns>
        private static bool ValidateAndFixConfiguration(HardwareConfig config)
        {
            if (config == null)
                return false;

            // 验证版本
            if (string.IsNullOrEmpty(config.Version))
                config.Version = "1.0";

            // 处理版本兼容性
            config = HandleVersionCompatibility(config);

            // 验证CPU配置
            if (config.Cpu == null)
                config.Cpu = new CpuConfig();

            if (config.Cpu.CoreCount <= 0 || config.Cpu.CoreCount > 256)
                config.Cpu.CoreCount = 16; // 默认值

            if (string.IsNullOrEmpty(config.Cpu.Model))
                config.Cpu.Model = "Intel(R) Core(TM) i9-12900K";

            if (string.IsNullOrEmpty(config.Cpu.CpuId))
                config.Cpu.CpuId = "BFEBFBFF000A06E9";

            // 验证硬盘配置
            if (config.Disk == null)
                config.Disk = new DiskConfig();

            if (string.IsNullOrEmpty(config.Disk.Serial))
                config.Disk.Serial = "1234567890ABCDEF";

            // 验证MAC配置
            if (config.Mac == null)
                config.Mac = new MacConfig();

            // 验证MAC地址格式
            if (!string.IsNullOrEmpty(config.Mac.Address))
            {
                string[] macParts = config.Mac.Address.Split(':');
                if (macParts.Length != 6)
                    config.Mac.Address = "00:11:22:33:44:55";
                else
                {
                    bool validMac = true;
                    foreach (string part in macParts)
                    {
                        if (!byte.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out _))
                        {
                            validMac = false;
                            break;
                        }
                    }
                    if (!validMac)
                        config.Mac.Address = "00:11:22:33:44:55";
                }
            }
            else
            {
                config.Mac.Address = "00:11:22:33:44:55";
            }

            // 验证主板配置
            if (config.Motherboard == null)
                config.Motherboard = new MotherboardConfig();

            if (string.IsNullOrEmpty(config.Motherboard.Serial))
                config.Motherboard.Serial = "MB-2025-12345678";

            return true;
        }

        /// <summary>
        /// 处理版本兼容性
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <returns>处理后的配置</returns>
        private static HardwareConfig HandleVersionCompatibility(HardwareConfig config)
        {
            try
            {
                // 解析版本号
                Version configVersion = new Version(config.Version);
                Version currentVersion = new Version("1.0");

                // 处理版本差异
                if (configVersion < currentVersion)
                {
                    // 旧版本配置的处理逻辑
                    // 例如：添加新字段的默认值
                    _logger?.Info($"Upgraded configuration from version {config.Version} to {currentVersion}");
                    config.Version = currentVersion.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to handle version compatibility", ex);
                config.Version = "1.0"; // 重置为默认版本
            }

            return config;
        }

        // 日志记录器（静态）
        private static ILogger _logger;

        /// <summary>
        /// 设置日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <param name="configPath">配置文件路径</param>
        /// <returns>保存结果</returns>
        public static bool SaveConfiguration(HardwareConfig config, string configPath)
        {
            try
            {
                // 检查配置是否为null
                if (config == null)
                    return false;

                // 检查配置路径是否为null
                if (string.IsNullOrEmpty(configPath))
                    return false;

                // 序列化配置
                string configContent = JsonConvert.SerializeObject(config, Formatting.Indented);

                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                // 保存配置文件
                File.WriteAllText(configPath, configContent);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 列出指定目录中的所有配置文件
        /// </summary>
        /// <param name="configDirectory">配置文件目录</param>
        /// <returns>配置文件路径列表</returns>
        public static string[] ListConfigurationFiles(string configDirectory)
        {
            try
            {
                if (!Directory.Exists(configDirectory))
                {
                    return new string[0];
                }

                return Directory.GetFiles(configDirectory, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <returns>创建结果</returns>
        public static bool CreateDefaultConfiguration(string configPath)
        {
            try
            {
                var defaultConfig = new HardwareConfig();
                return SaveConfiguration(defaultConfig, configPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
