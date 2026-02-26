using System;
using System.Runtime.InteropServices;

namespace HardwareHook.Core.Native
{
    /// <summary>
    /// 原生API定义
    /// </summary>
    public static class NativeApi
    {
        #region 系统信息
        
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }
        
        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        
        [DllImport("kernel32.dll")]
        public static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);
        
        #endregion
        
        #region 磁盘信息
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetVolumeInformationW(
            string lpRootPathName,
            IntPtr lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            IntPtr lpFileSystemNameBuffer,
            int nFileSystemNameSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);
        
        #endregion
        
        #region 网络信息
        
        [StructLayout(LayoutKind.Sequential)]
        public struct IP_ADDR_STRING
        {
            public IntPtr Next;
            public IP_ADDRESS_STRING IpAddress;
            public IP_ADDRESS_STRING IpMask;
            public uint Context;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct IP_ADDRESS_STRING
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string Address;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct IP_ADAPTER_INFO
        {
            public IntPtr Next;
            public uint ComboIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string AdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 132)]
            public string Description;
            public uint AddressLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Address;
            public uint Index;
            public uint Type;
            public uint DhcpEnabled;
            public IntPtr CurrentIpAddress;
            public IP_ADDR_STRING IpAddressList;
            public IP_ADDR_STRING GatewayList;
            public IP_ADDR_STRING DhcpServer;
            public byte HaveWins;
            public IP_ADDR_STRING PrimaryWinsServer;
            public IP_ADDR_STRING SecondaryWinsServer;
            public uint LeaseObtained;
            public uint LeaseExpires;
        }
        
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdaptersInfo(IntPtr pAdapterInfo, ref int pOutBufLen);
        
        #endregion
        
        #region 注册表
        
        public static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);
        
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int RegOpenKeyExW(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            uint samDesired,
            out IntPtr phkResult);
        
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegQueryValueExW(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            out uint lpType,
            IntPtr lpData,
            ref uint lpcbData);
        
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);
        
        #endregion
        
        #region 内存操作
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree(
            IntPtr lpAddress,
            uint dwSize,
            uint dwFreeType);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);
        
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
        
        #endregion
        
        #region 事件
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateEventW(
            IntPtr lpEventAttributes,
            bool bManualReset,
            bool bInitialState,
            string lpName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetEvent(IntPtr hEvent);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ResetEvent(IntPtr hEvent);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
        
        #endregion
    }
}
