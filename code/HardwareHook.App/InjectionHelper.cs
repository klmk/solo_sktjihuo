using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
                // 架构校验：本进程须为 64 位，且仅支持向 64 位进程注入，否则 EasyHook 易报 Code 15
                if (!IsCurrentProcess64Bit())
                {
                    errorMessage = "当前主程序为 32 位。向 64 位进程注入会失败（错误 Code 15）。请使用 x64 构建并运行主程序。";
                    logger.Error(errorMessage, module: "Injection");
                    return false;
                }

                if (!IsTargetProcess64Bit(processId, out var targetBitnessMsg))
                {
                    errorMessage = $"目标进程 (PID={processId}) 不是 64 位进程，或无法查询。{targetBitnessMsg} 仅支持向 64 位进程注入。";
                    logger.Error(errorMessage, module: "Injection");
                    return false;
                }

                var coreDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HardwareHook.Core.dll");
                if (!File.Exists(coreDllPath))
                {
                    errorMessage = $"未找到核心 DLL：{coreDllPath}";
                    logger.Error(errorMessage, module: "Injection");
                    return false;
                }

                uninstallEventName = $"HardwareHook.Uninstall.{processId}.{Guid.NewGuid():N}";

                // 将配置复制到 ProgramData，确保目标进程（可能不同用户/权限）一定能读到
                var programDataConfigDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "HardwareHook", "Config");
                Directory.CreateDirectory(programDataConfigDir);
                var configForInject = Path.Combine(programDataConfigDir, $"inject_pid{processId}.json");
                try
                {
                    File.Copy(Path.GetFullPath(configPath), configForInject, overwrite: true);
                }
                catch (Exception ex)
                {
                    errorMessage = $"复制配置文件到 {configForInject} 失败：{ex.Message}";
                    logger.Error(errorMessage, ex, "Injection");
                    return false;
                }

                RemoteHooking.Inject(
                    processId,
                    InjectionOptions.Default,
                    coreDllPath,
                    coreDllPath,
                    configForInject,
                    uninstallEventName);

                logger.Info($"成功向进程 {processId} 注入 Hook。卸载事件：{uninstallEventName}", "Injection");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = BuildInjectErrorMessage(processId, ex);
                logger.Error(errorMessage, ex, "Injection");
                return false;
            }
        }

        private static string BuildInjectErrorMessage(int processId, Exception ex)
        {
            var msg = ex.Message ?? "";
            if (msg.IndexOf("Code: 15", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("STATUS_INTERNAL_ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("injected C++ completion routine", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"注入进程 {processId} 失败：{msg}\n\n常见原因：主程序与目标进程架构不一致（例如 32 位主程序注入 64 位进程）。请使用 x64 构建的主程序，并仅对 64 位进程注入。";
            }
            return $"注入进程 {processId} 失败：{msg}";
        }

        private static bool IsCurrentProcess64Bit()
        {
            return IntPtr.Size == 8;
        }

        private static bool IsTargetProcess64Bit(int processId, out string message)
        {
            message = "";
            try
            {
                using (var proc = Process.GetProcessById(processId))
                {
                    if (IsProcess64Bit(proc))
                    {
                        return true;
                    }
                    message = "目标进程为 32 位";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = "无法打开目标进程：" + ex.Message;
                return false;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        private static bool IsProcess64Bit(Process process)
        {
            try
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    if (IsWow64Process(process.Handle, out var isWow64))
                    {
                        return !isWow64; // 64 位进程上 IsWow64Process 返回 false
                    }
                }
                return IntPtr.Size == 8;
            }
            catch
            {
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

