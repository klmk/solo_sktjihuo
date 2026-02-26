using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using HardwareHook.Core.HardwareInfo;
using HardwareHook.Core.Logging;

namespace HardwareHook.Tester
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [STAThread]
        private static void Main(string[] args)
        {
            // 有命令行参数：控制台模式（可为从 cmd 启动时分配控制台并输出）
            if (args != null && args.Length > 0)
            {
                RunConsoleMode(args);
                return;
            }

            // 无参数：启动 GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TesterMainForm());
        }

        private static void RunConsoleMode(string[] args)
        {
            if (!AllocConsole())
                AllocConsole(); // 若已有关联控制台则忽略

            Console.OutputEncoding = Encoding.UTF8;

            var logDir = Environment.CurrentDirectory;
            ILogger logger = new FileLogger(logDir, "tester");
            var snapshot = HardwareInfoReader.GetCurrent(logger);

            var set = args.Select(a => a.ToLowerInvariant().Trim()).ToHashSet();

            if (set.Contains("--help") || set.Contains("-h"))
            {
                PrintHelp();
                WaitAndExit();
                return;
            }

            bool any = false;
            if (set.Contains("--cpu") || set.Contains("-c")) { PrintCpu(snapshot); any = true; }
            if (set.Contains("--disk") || set.Contains("-d")) { PrintDisk(snapshot); any = true; }
            if (set.Contains("--mac") || set.Contains("-m")) { PrintMac(snapshot); any = true; }
            if (set.Contains("--bios") || set.Contains("--mb") || set.Contains("-b")) { PrintMotherboard(snapshot); any = true; }
            if (set.Contains("--all") || set.Contains("-a")) { PrintAll(snapshot); any = true; }

            if (!any)
            {
                PrintAll(snapshot);
            }

            WaitAndExit();
        }

        private static void WaitAndExit()
        {
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            try { Console.ReadKey(true); } catch { }
            FreeConsole();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("硬件信息测试程序");
            Console.WriteLine();
            Console.WriteLine("用法: HardwareHook.Tester.exe [选项]");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  --help, -h           显示此帮助信息");
            Console.WriteLine("  --cpu, -c            仅输出 CPU 信息");
            Console.WriteLine("  --disk, -d           仅输出硬盘信息");
            Console.WriteLine("  --mac, -m            仅输出 MAC 地址");
            Console.WriteLine("  --bios, -b, --mb     仅输出主板信息");
            Console.WriteLine("  --all, -a            输出全部硬件信息（默认）");
            Console.WriteLine();
            Console.WriteLine("无参数启动时打开图形界面。");
        }

        private static void PrintAll(HardwareInfoSnapshot s)
        {
            PrintCpu(s);
            PrintDisk(s);
            PrintMac(s);
            PrintMotherboard(s);
        }

        private static void PrintCpu(HardwareInfoSnapshot s)
        {
            Console.WriteLine("=== CPU 信息 ===");
            Console.WriteLine("型号: " + s.CpuModel);
            Console.WriteLine("核心数: " + s.CpuCoreCount);
            Console.WriteLine("CpuId: " + s.CpuId);
            Console.WriteLine();
        }

        private static void PrintDisk(HardwareInfoSnapshot s)
        {
            Console.WriteLine("=== 硬盘信息 ===");
            Console.WriteLine("序列号: " + s.DiskSerial);
            Console.WriteLine();
        }

        private static void PrintMac(HardwareInfoSnapshot s)
        {
            Console.WriteLine("=== 网络信息 ===");
            Console.WriteLine("MAC 地址: " + s.MacAddress);
            Console.WriteLine();
        }

        private static void PrintMotherboard(HardwareInfoSnapshot s)
        {
            Console.WriteLine("=== 主板信息 ===");
            Console.WriteLine("主板序列号: " + s.MotherboardSerial);
            Console.WriteLine();
        }
    }
}
