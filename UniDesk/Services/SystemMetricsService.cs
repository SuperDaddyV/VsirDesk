using UniDesk.Models;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace UniDesk.Services;

public sealed class SystemMetricsService : ISystemMetricsService, IDisposable
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly AsusHardwareReader _asusReader = new();
    private readonly AmdAdlReader _amdReader = new();
    private readonly NetworkSpeedReader _networkReader = new();
    private bool _disposed;

    public SystemMetricsService()
    {
        try { _cpuCounter.NextValue(); } catch { }
    }

    public SystemMetricsSnapshot Read()
    {
        var amd = _amdReader.Read();
        var network = _networkReader.Read();
        return new SystemMetricsSnapshot
        {
            CpuUsage = SafeValue(() => _cpuCounter.NextValue()),
            CpuTemperature = _asusReader.ReadCpuPackageTemperature(),
            MemoryUsage = ReadMemoryUsage(),
            GpuUsage = amd.GpuUsage,
            GpuTemperature = amd.GpuTemperature,
            NetworkReceivedBytesPerSecond = network.ReceivedBytesPerSecond,
            NetworkSentBytesPerSecond = network.SentBytesPerSecond
        };
    }

    private static double? SafeValue(Func<float> read)
    {
        try { return Clamp(read(), 0, 100); }
        catch { return null; }
    }

    private static double? ReadMemoryUsage()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status) || status.ullTotalPhys == 0) return null;
        var used = 1d - status.ullAvailPhys / (double)status.ullTotalPhys;
        return Clamp(used * 100d, 0, 100);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cpuCounter.Dispose();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MemoryStatusEx()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
        }
    }

    private sealed class AsusHardwareReader
    {
        private const int TemperatureDataType = 3;
        private const int SensorRecordSize = 0x88;
        private const int CurrentValueOffset = 0x04;
        private const int SensorNameOffset = 0x24;
        private HMGetData2? _getData2;
        private bool _initialized;

        public double? ReadCpuPackageTemperature()
        {
            try
            {
                if (!EnsureInitialized() || _getData2 == null) return null;

                var count = _getData2(TemperatureDataType, IntPtr.Zero);
                if (count <= 0 || count > 64) return null;

                var buffer = Marshal.AllocHGlobal(count * SensorRecordSize);
                try
                {
                    for (var i = 0; i < count * SensorRecordSize; i++) Marshal.WriteByte(buffer, i, 0);
                    if (_getData2(TemperatureDataType, buffer) <= 0) return null;

                    double? cpu = null;
                    for (var i = 0; i < count; i++)
                    {
                        var record = IntPtr.Add(buffer, i * SensorRecordSize);
                        var name = ReadWideString(IntPtr.Add(record, SensorNameOffset), 48);
                        var temp = Marshal.ReadInt32(IntPtr.Add(record, CurrentValueOffset)) / 10.0;
                        if (temp <= 0 || temp >= 120) continue;

                        if (name.IndexOf("CPU Package", StringComparison.OrdinalIgnoreCase) >= 0) return temp;
                        if (!cpu.HasValue && name.Equals("CPU", StringComparison.OrdinalIgnoreCase)) cpu = temp;
                    }

                    return cpu;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                return null;
            }
        }

        private bool EnsureInitialized()
        {
            if (_initialized) return _getData2 != null;
            _initialized = true;

            var home = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"ASUS\Armoury Crate Service\MB_Home");
            var library = Path.Combine(home, "aaHMLib_x64.dll");
            if (!File.Exists(library)) return false;

            SetDllDirectory(home);
            var handle = LoadLibrary(library);
            if (handle == IntPtr.Zero) return false;

            var proc = GetProcAddress(handle, "HM_GetData2");
            if (proc == IntPtr.Zero) return false;

            _getData2 = Marshal.GetDelegateForFunctionPointer<HMGetData2>(proc);
            return true;
        }

        private static string ReadWideString(IntPtr ptr, int maxChars)
        {
            var bytes = new byte[maxChars * 2];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);
            var end = 0;
            for (; end + 1 < bytes.Length; end += 2)
            {
                if (bytes[end] == 0 && bytes[end + 1] == 0) break;
            }

            return Encoding.Unicode.GetString(bytes, 0, end);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int HMGetData2(int dataType, IntPtr data);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string path);
    }

    private sealed class AmdAdlReader
    {
        private const int SensorGpuTemperatureEdge = 8;
        private const int SensorGpuActivity = 19;
        private readonly ADLMainMemoryAlloc _allocCallback = Alloc;
        private bool _initialized;
        private IntPtr _context = IntPtr.Zero;

        public AmdMetrics Read()
        {
            try
            {
                if (!EnsureInitialized()) return AmdMetrics.Empty;
                if (ADL2_Adapter_NumberOfAdapters_Get(_context, out var count) != 0 || count <= 0) return AmdMetrics.Empty;

                var size = Marshal.SizeOf<AdapterInfo>();
                var ptr = Marshal.AllocHGlobal(size * count);
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        Marshal.StructureToPtr(new AdapterInfo { iSize = size }, IntPtr.Add(ptr, i * size), false);
                    }

                    if (ADL2_Adapter_AdapterInfo_Get(_context, ptr, size * count) != 0) return AmdMetrics.Empty;

                    for (var i = 0; i < count; i++)
                    {
                        var info = Marshal.PtrToStructure<AdapterInfo>(IntPtr.Add(ptr, i * size));
                        if (string.IsNullOrEmpty(info.strAdapterName) ||
                            info.strAdapterName.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        ADL2_Adapter_Active_Get(_context, info.iAdapterIndex, out var active);
                        if (active != 1 && i != 0) continue;

                        var data = new ADLPMLogDataOutput
                        {
                            size = Marshal.SizeOf<ADLPMLogDataOutput>(),
                            sensors = new ADLSingleSensorData[256]
                        };

                        if (ADL2_New_QueryPMLogData_Get(_context, info.iAdapterIndex, ref data) != 0) continue;

                        double? temp = null;
                        double? usage = null;
                        if (data.sensors[SensorGpuTemperatureEdge].supported != 0)
                            temp = data.sensors[SensorGpuTemperatureEdge].value;
                        if (data.sensors[SensorGpuActivity].supported != 0)
                            usage = Clamp(data.sensors[SensorGpuActivity].value, 0, 100);

                        if (temp.HasValue || usage.HasValue) return new AmdMetrics(usage, temp);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch
            {
            }

            return AmdMetrics.Empty;
        }

        private bool EnsureInitialized()
        {
            if (_initialized) return _context != IntPtr.Zero;
            _initialized = true;
            try
            {
                return ADL2_Main_Control_Create(_allocCallback, 1, out _context) == 0 && _context != IntPtr.Zero;
            }
            catch
            {
                _context = IntPtr.Zero;
                return false;
            }
        }

        private static IntPtr Alloc(int size) => Marshal.AllocHGlobal(size);

        private delegate IntPtr ADLMainMemoryAlloc(int size);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Create(ADLMainMemoryAlloc callback, int enumConnectedAdapters, out IntPtr context);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Adapter_NumberOfAdapters_Get(IntPtr context, out int count);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Adapter_AdapterInfo_Get(IntPtr context, IntPtr info, int inputSize);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Adapter_Active_Get(IntPtr context, int adapterIndex, out int active);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_New_QueryPMLogData_Get(IntPtr context, int adapterIndex, ref ADLPMLogDataOutput output);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct AdapterInfo
        {
            public int iSize;
            public int iAdapterIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strUDID;
            public int iBusNumber;
            public int iDeviceNumber;
            public int iFunctionNumber;
            public int iVendorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strAdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strDisplayName;
            public int iPresent;
            public int iExist;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strDriverPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strDriverPathExt;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strPNPString;
            public int iOSDisplayIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ADLSingleSensorData
        {
            public int supported;
            public int value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ADLPMLogDataOutput
        {
            public int size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ADLSingleSensorData[] sensors;
        }
    }

    private readonly struct AmdMetrics(double? gpuUsage, double? gpuTemperature)
    {
        public static readonly AmdMetrics Empty = new(null, null);
        public double? GpuUsage { get; } = gpuUsage;
        public double? GpuTemperature { get; } = gpuTemperature;
    }

    private sealed class NetworkSpeedReader
    {
        private static readonly string[] VirtualAdapterKeywords =
        [
            "virtual",
            "loopback",
            "pseudo",
            "hyper-v",
            "vethernet",
            "vmware",
            "virtualbox",
            "docker",
            "wintun",
            "tap",
            "npcap",
            "vpn",
            "wireguard",
            "tailscale",
            "zerotier",
            "bluetooth"
        ];

        private NetworkSample? _previous;

        public NetworkSpeed Read()
        {
            try
            {
                var current = ReadSample();
                var previous = _previous;
                _previous = current;

                if (previous == null) return NetworkSpeed.Zero;

                var seconds = (current.Timestamp - previous.Timestamp).TotalSeconds;
                var receivedDelta = current.ReceivedBytes - previous.ReceivedBytes;
                var sentDelta = current.SentBytes - previous.SentBytes;

                if (seconds <= 0 || receivedDelta < 0 || sentDelta < 0)
                {
                    return NetworkSpeed.Zero;
                }

                return new NetworkSpeed(receivedDelta / seconds, sentDelta / seconds);
            }
            catch
            {
                return NetworkSpeed.Empty;
            }
        }

        private static NetworkSample ReadSample()
        {
            double received = 0;
            double sent = 0;

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!IsUsableAdapter(adapter)) continue;

                try
                {
                    var stats = adapter.GetIPv4Statistics();
                    received += Math.Max(0, stats.BytesReceived);
                    sent += Math.Max(0, stats.BytesSent);
                }
                catch
                {
                }
            }

            return new NetworkSample(DateTimeOffset.UtcNow, received, sent);
        }

        private static bool IsUsableAdapter(NetworkInterface adapter)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up) return false;

            var type = adapter.NetworkInterfaceType;
            if (type is NetworkInterfaceType.Loopback or
                NetworkInterfaceType.Tunnel or
                NetworkInterfaceType.Unknown)
            {
                return false;
            }

            if (type is not (NetworkInterfaceType.Ethernet or
                NetworkInterfaceType.FastEthernetFx or
                NetworkInterfaceType.FastEthernetT or
                NetworkInterfaceType.GigabitEthernet or
                NetworkInterfaceType.Wireless80211))
            {
                return false;
            }

            var text = $"{adapter.Name} {adapter.Description}".ToLowerInvariant();
            return !VirtualAdapterKeywords.Any(text.Contains);
        }
    }

    private sealed record NetworkSample(DateTimeOffset Timestamp, double ReceivedBytes, double SentBytes);

    private readonly struct NetworkSpeed(double? receivedBytesPerSecond, double? sentBytesPerSecond)
    {
        public static readonly NetworkSpeed Empty = new(null, null);
        public static readonly NetworkSpeed Zero = new(0, 0);
        public double? ReceivedBytesPerSecond { get; } = receivedBytesPerSecond;
        public double? SentBytesPerSecond { get; } = sentBytesPerSecond;
    }
}
