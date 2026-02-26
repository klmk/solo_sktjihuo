using System;
using System.IO;
using System.Threading;
using EasyHook;
using HardwareHook.Core.Logging;

namespace HardwareHook.App
{
    internal static class InjectionHelper
    {
        public static bool TryInject(
            int processId,
            string configPath,
            ILogger logger,
            out string uninstallEventName,
            out string errorMessage)
        {
            uninstallEventName = string.Empty;
            errorMessage = string.Empty;

            try
            {
                var coreDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HardwareHook.Core.dll");
                if (!File.Exists(coreDllPath))
                {
                    errorMessage = $"未找到核心 DLL：{coreDllPath}";
                    logger.Error(errorMessage, module: "Injection");
                    return false;
                }

                uninstallEventName = $"HardwareHook.Uninstall.{processId}.{Guid.NewGuid():N}";

                var configFullPath = Path.GetFullPath(configPath);
                RemoteHooking.Inject(
                    processId,
                    InjectionOptions.Default,
                    coreDllPath,
                    coreDllPath,
                    configFullPath,
                    uninstallEventName);

                logger.Info($"成功向进程 {processId} 注入 Hook。卸载事件：{uninstallEventName}", "Injection");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"注入进程 {processId} 失败：{ex.Message}";
                logger.Error(errorMessage, ex, "Injection");
                return false;
            }
        }

        public static bool TryUninstall(
            string uninstallEventName,
            ILogger logger,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(uninstallEventName))
            {
                errorMessage = "卸载事件名称为空。";
                return false;
            }

            try
            {
                using (var evt = EventWaitHandle.OpenExisting(uninstallEventName))
                {
                    evt.Set();
                }

                logger.Info($"已发送卸载事件：{uninstallEventName}", "Injection");
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                errorMessage = $"未找到卸载事件：{uninstallEventName}";
                logger.Warning(errorMessage, "Injection");
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"发送卸载事件 {uninstallEventName} 失败：{ex.Message}";
                logger.Error(errorMessage, ex, "Injection");
                return false;
            }
        }
    }
}

