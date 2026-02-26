using System;
using System.Windows.Forms;
using HardwareHook.Core.HardwareInfo;

namespace HardwareHook.Tester
{
    /// <summary>
    /// 测试程序入口类
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // 检查命令行参数
            if (args.Length > 0)
            {
                // 命令行模式
                RunCommandLine(args);
            }
            else
            {
                // GUI模式
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TesterMainForm());
            }
        }

        /// <summary>
        /// 运行命令行模式
        /// </summary>
        /// <param name="args">命令行参数</param>
        private static void RunCommandLine(string[] args)
        {
            string command = args[0].ToLower();

            switch (command)
            {
                case "--cpu":
                    TestCpuInfo();
                    break;
                case "--disk":
                    TestDiskInfo();
                    break;
                case "--mac":
                    TestMacInfo();
                    break;
                case "--bios":
                    TestMotherboardInfo();
                    break;
                case "--all":
                    TestAllInfo();
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        /// <summary>
        /// 测试CPU信息
        /// </summary>
        private static void TestCpuInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            Console.WriteLine("=== CPU 信息测试 ===");
            Console.WriteLine($"CPU型号: {snapshot.CpuModel}");
            Console.WriteLine($"核心数: {snapshot.CpuCoreCount}");
            Console.WriteLine($"CPU ID: {snapshot.CpuId}");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试硬盘信息
        /// </summary>
        private static void TestDiskInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            Console.WriteLine("=== 硬盘 信息测试 ===");
            Console.WriteLine($"硬盘序列号: {snapshot.DiskSerial}");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试MAC地址信息
        /// </summary>
        private static void TestMacInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            Console.WriteLine("=== MAC 地址测试 ===");
            Console.WriteLine($"MAC地址: {snapshot.MacAddress}");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试主板信息
        /// </summary>
        private static void TestMotherboardInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            Console.WriteLine("=== 主板 信息测试 ===");
            Console.WriteLine($"主板序列号: {snapshot.MotherboardSerial}");
            Console.WriteLine();
        }

        /// <summary>
        /// 测试所有信息
        /// </summary>
        private static void TestAllInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            Console.WriteLine("=== 所有硬件信息测试 ===");
            Console.WriteLine(snapshot.ToString());
            Console.WriteLine();
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("Windows硬件信息模拟测试工具");
            Console.WriteLine("用法:");
            Console.WriteLine("  HardwareHook.Tester --cpu     测试CPU信息");
            Console.WriteLine("  HardwareHook.Tester --disk    测试硬盘信息");
            Console.WriteLine("  HardwareHook.Tester --mac     测试MAC地址");
            Console.WriteLine("  HardwareHook.Tester --bios    测试主板信息");
            Console.WriteLine("  HardwareHook.Tester --all     测试所有信息");
            Console.WriteLine();
        }
    }
}
