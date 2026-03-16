using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetVanguard.Core.Models
{
    public class NetworkProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public NetworkInterfaceType InterfaceType { get; set; }
        
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
        
        public long? QuotaLimitBytes { get; set; }
        public bool IsQuotaExceeded => 
            QuotaLimitBytes.HasValue && (TotalBytesSent + TotalBytesReceived) >= QuotaLimitBytes.Value;
    }

    public class NetworkApplication
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public bool IsWindowsService { get; set; }
        
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        public bool IsBlocked { get; set; }
        public long? BandwidthLimitKbps { get; set; }
    }

    public class NetworkConnection
    {
        public string RemoteAddress { get; set; } = string.Empty;
        public int RemotePort { get; set; }
        public string LocalAddress { get; set; } = string.Empty;
        public int LocalPort { get; set; }
        public ProtocolType Protocol { get; set; }

        public int OwningProcessId { get; set; }
        
        public long CurrentDownloadSpeedBps { get; set; }
        public long CurrentUploadSpeedBps { get; set; }
    }
}
