using System;
using System.IO;
using Newtonsoft.Json;

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

                // 验证配置
                if (!ValidateConfiguration(config))
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
        /// 验证配置
        /// </summary>
        /// <param name="config">硬件配置</param>
        /// <returns>验证结果</returns>
        private static bool ValidateConfiguration(HardwareConfig config)
        {
            if (config == null)
                return false;

            // 验证版本
            if (string.IsNullOrEmpty(config.Version))
                return false;

            // 验证CPU配置
            if (config.Cpu == null)
                return false;

            if (config.Cpu.CoreCount <= 0)
                return false;

            // 验证硬盘配置
            if (config.Disk == null)
                return false;

            // 验证MAC配置
            if (config.Mac == null)
                return false;

            // 验证主板配置
            if (config.Motherboard == null)
                return false;

            return true;
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
