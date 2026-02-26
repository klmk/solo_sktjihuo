using System;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using CommandLine;

namespace HardwareInfo.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Windows硬件信息测试程序");
            Console.WriteLine("=======================\n");
            
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }
        
        static void RunOptions(Options opts)
        {
            try
            {
                if (opts.All || opts.Cpu)
                {
                    ShowCpuInfo();
                    Console.WriteLine();
                }
                
                if (opts.All || opts.Disk)
                {
                    ShowDiskInfo();
                    Console.WriteLine();
                }
                
                if (opts.All || opts.Mac)
                {
                    ShowMacInfo();
                    Console.WriteLine();
                }
                
                if (opts.All || opts.Bios)
                {
                    ShowBiosInfo();
                    Console.WriteLine();
                }
                
                if (opts.All || opts.Memory)
                {
                    ShowMemoryInfo();
                    Console.WriteLine();
                }
                
                if (opts.All || opts.System)
                {
                    ShowSystemInfo();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"错误: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
        
        static void HandleParseError(System.Collections.Generic.IEnumerable<Error> errs)
        {
            Environment.Exit(1);
        }
        
        static void ShowCpuInfo()
        {
            Console.WriteLine("CPU 信息:");
            Console.WriteLine("--------");
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Console.WriteLine($"  名称: {obj["Name"]}");
                        Console.WriteLine($"  核心数: {obj["NumberOfCores"]}");
                        Console.WriteLine($"  线程数: {obj["NumberOfLogicalProcessors"]}");
                        Console.WriteLine($"  处理器ID: {obj["ProcessorId"]}");
                        Console.WriteLine($"  架构: {GetArchitecture((ushort)obj["Architecture"])}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static void ShowDiskInfo()
        {
            Console.WriteLine("硬盘 信息:");
            Console.WriteLine("---------");
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    int index = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        index++;
                        Console.WriteLine($"  硬盘 {index}:");
                        Console.WriteLine($"    型号: {obj["Model"]}");
                        Console.WriteLine($"    序列号: {obj["SerialNumber"]}");
                        Console.WriteLine($"    大小: {FormatBytes((ulong)obj["Size"])}");
                    }
                }
                
                // 获取卷序列号
                string drive = Path.GetPathRoot(Environment.SystemDirectory);
                uint serialNum, maxCompLen, fileSystemFlags;
                if (GetVolumeInformationW(drive, IntPtr.Zero, 0, out serialNum, out maxCompLen, out fileSystemFlags, IntPtr.Zero, 0))
                {
                    Console.WriteLine($"  系统卷序列号: {serialNum:X8}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static void ShowMacInfo()
        {
            Console.WriteLine("网络适配器 信息:");
            Console.WriteLine("---------------");
            
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up || 
                        nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        Console.WriteLine($"  {nic.Name} ({nic.NetworkInterfaceType}):");
                        Console.WriteLine($"    MAC地址: {nic.GetPhysicalAddress()}");
                        Console.WriteLine($"    速度: {FormatSpeed(nic.Speed)}");
                        Console.WriteLine($"    状态: {nic.OperationalStatus}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static void ShowBiosInfo()
        {
            Console.WriteLine("主板/BIOS 信息:");
            Console.WriteLine("--------------");
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Console.WriteLine($"  BIOS版本: {obj["BIOSVersion"]}");
                        Console.WriteLine($"  制造商: {obj["Manufacturer"]}");
                        Console.WriteLine($"  序列号: {obj["SerialNumber"]}");
                        Console.WriteLine($"  发布日期: {obj["ReleaseDate"]}");
                    }
                }
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Console.WriteLine($"  主板型号: {obj["Product"]}");
                        Console.WriteLine($"  主板制造商: {obj["Manufacturer"]}");
                        Console.WriteLine($"  主板序列号: {obj["SerialNumber"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static void ShowMemoryInfo()
        {
            Console.WriteLine("内存 信息:");
            Console.WriteLine("--------");
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    ulong total = 0;
                    int count = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        count++;
                        ulong capacity = (ulong)obj["Capacity"];
                        total += capacity;
                        Console.WriteLine($"  内存条 {count}:");
                        Console.WriteLine($"    容量: {FormatBytes(capacity)}");
                        Console.WriteLine($"    速度: {obj["Speed"]} MHz");
                        Console.WriteLine($"    制造商: {obj["Manufacturer"]}");
                    }
                    Console.WriteLine($"  总容量: {FormatBytes(total)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static void ShowSystemInfo()
        {
            Console.WriteLine("系统 信息:");
            Console.WriteLine("--------");
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Console.WriteLine($"  计算机名称: {obj["Name"]}");
                        Console.WriteLine($"  总物理内存: {FormatBytes((ulong)obj["TotalPhysicalMemory"])}");
                        Console.WriteLine($"  系统类型: {obj["SystemType"]}");
                        Console.WriteLine($"  制造商: {obj["Manufacturer"]}");
                        Console.WriteLine($"  型号: {obj["Model"]}");
                    }
                }
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Console.WriteLine($"  操作系统: {obj["Caption"]}");
                        Console.WriteLine($"  版本: {obj["Version"]}");
                        Console.WriteLine($"  架构: {obj["OSArchitecture"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  获取失败: {ex.Message}");
            }
        }
        
        static string GetArchitecture(ushort arch)
        {
            return arch switch
            {
                0 => "x86",
                1 => "MIPS",
                2 => "Alpha",
                3 => "PowerPC",
                6 => "Itanium",
                9 => "x64",
                _ => $"Unknown ({arch})"
            };
        }
        
        static string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        static string FormatSpeed(long speed)
        {
            if (speed >= 1000000000)
                return $"{speed / 1000000000.0:0.0} Gbps";
            if (speed >= 1000000)
                return $"{speed / 1000000.0:0.0} Mbps";
            if (speed >= 1000)
                return $"{speed / 1000.0:0.0} Kbps";
            return $"{speed} bps";
        }
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetVolumeInformationW(
            string lpRootPathName,
            IntPtr lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            IntPtr lpFileSystemNameBuffer,
            int nFileSystemNameSize);
    }
    
    class Options
    {
        [Option('a', "all", Default = false, HelpText = "显示所有硬件信息")]
        public bool All { get; set; }
        
        [Option("cpu", Default = false, HelpText = "显示CPU信息")]
        public bool Cpu { get; set; }
        
        [Option("disk", Default = false, HelpText = "显示硬盘信息")]
        public bool Disk { get; set; }
        
        [Option("mac", Default = false, HelpText = "显示MAC地址")]
        public bool Mac { get; set; }
        
        [Option("bios", Default = false, HelpText = "显示主板/BIOS信息")]
        public bool Bios { get; set; }
        
        [Option("memory", Default = false, HelpText = "显示内存信息")]
        public bool Memory { get; set; }
        
        [Option("system", Default = false, HelpText = "显示系统信息")]
        public bool System { get; set; }
    }
}
