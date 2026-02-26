using System;
using System.IO;
using Newtonsoft.Json;
using HardwareHook.Core.Logging;

namespace HardwareHook.Core.Configuration
{
    public static class ConfigurationLoader
    {
        public static ConfigurationLoadResult Load(string configPath, ILogger? logger = null)
        {
            var result = new ConfigurationLoadResult();

            if (string.IsNullOrWhiteSpace(configPath))
            {
                result.Success = false;
                result.ErrorMessage = "配置文件路径为空。";
                result.ValidationErrors.Add(result.ErrorMessage);
                return result;
            }

            try
            {
                if (!File.Exists(configPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"配置文件不存在：{configPath}";
                    result.ValidationErrors.Add(result.ErrorMessage);
                    logger?.Error(result.ErrorMessage, module: "Config");
                    return result;
                }

                var json = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    result.Success = false;
                    result.ErrorMessage = "配置文件内容为空。";
                    result.ValidationErrors.Add(result.ErrorMessage);
                    logger?.Error(result.ErrorMessage, module: "Config");
                    return result;
                }

                var config = JsonConvert.DeserializeObject<HardwareConfig>(json);
                if (config == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "配置文件解析失败。";
                    result.ValidationErrors.Add(result.ErrorMessage);
                    logger?.Error(result.ErrorMessage, module: "Config");
                    return result;
                }

                Validate(config, result);

                result.Config = config;
                if (result.ValidationErrors.Count > 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "配置文件验证失败，请检查配置项。";
                    logger?.Error(result.ErrorMessage, module: "Config");
                }
                else
                {
                    result.Success = true;
                    logger?.Info("配置文件加载并验证成功。", module: "Config");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"加载配置文件时发生异常：{ex.Message}";
                result.ValidationErrors.Add(result.ErrorMessage);
                logger?.Error("加载配置文件时发生异常。", ex, "Config");
            }

            return result;
        }

        private static void Validate(HardwareConfig config, ConfigurationLoadResult result)
        {
            if (config.Cpu == null)
            {
                result.ValidationErrors.Add("Cpu 配置节缺失。");
            }
            else
            {
                if (config.Cpu.CoreCount <= 0)
                {
                    result.ValidationErrors.Add("Cpu.CoreCount 必须大于 0。");
                }

                if (string.IsNullOrWhiteSpace(config.Cpu.Model))
                {
                    result.ValidationErrors.Add("Cpu.Model 不能为空。");
                }
            }

            if (config.Disk == null || string.IsNullOrWhiteSpace(config.Disk.Serial))
            {
                result.ValidationErrors.Add("Disk.Serial 不能为空。");
            }

            if (config.Mac == null || string.IsNullOrWhiteSpace(config.Mac.Address))
            {
                result.ValidationErrors.Add("Mac.Address 不能为空。");
            }

            if (config.Motherboard == null || string.IsNullOrWhiteSpace(config.Motherboard.Serial))
            {
                result.ValidationErrors.Add("Motherboard.Serial 不能为空。");
            }

            // Version 字段做基本检查，后续可以扩展版本兼容策略
            if (string.IsNullOrWhiteSpace(config.Version))
            {
                result.ValidationErrors.Add("Version 不能为空。");
            }
        }
    }
}

