using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NetVanguard.Core.Models
{
    public enum ShowcaseType
    {
        Process,
        Detailed, // Excludes services, DLLs, etc.
        Adapter,
        Domain
    }

    public partial class NetworkProfile : ObservableObject
    {
        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public NetworkInterfaceType InterfaceType { get; set; }
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
        
        public long? QuotaLimitBytes { get; set; }
        public bool IsQuotaExceeded => 
            QuotaLimitBytes.HasValue && (TotalBytesSent + TotalBytesReceived) >= QuotaLimitBytes.Value;
    }

    public partial class NetworkApplication : ObservableObject
    {
        public int ProcessId { get; set; }
        
        private string _processName = string.Empty;
        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        private string _executablePath = string.Empty;
        public string ExecutablePath
        {
            get => _executablePath;
            set => SetProperty(ref _executablePath, value);
        }

        private bool _isWindowsService;
        public bool IsWindowsService
        {
            get => _isWindowsService;
            set => SetProperty(ref _isWindowsService, value);
        }
        
        private long _bytesSent;
        public long BytesSent
        {
            get => _bytesSent;
            set
            {
                if (SetProperty(ref _bytesSent, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesSent));
                }
            }
        }

        private long _bytesReceived;
        public long BytesReceived
        {
            get => _bytesReceived;
            set
            {
                if (SetProperty(ref _bytesReceived, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesReceived));
                }
            }
        }

        private DateTime _lastSeen = DateTime.Now;
        public DateTime LastSeen
        {
            get => _lastSeen;
            set => SetProperty(ref _lastSeen, value);
        }

        private bool _isBlocked;
        public bool IsBlocked
        {
            get => _isBlocked;
            set => SetProperty(ref _isBlocked, value);
        }

        public long? BandwidthLimitKbps { get; set; }

        public string DisplayBytesReceived => FormatTraffic(BytesReceived);
        public string DisplayBytesSent => FormatTraffic(BytesSent);

        public static string FormatTraffic(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    public partial class AdapterTraffic : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _interfaceId = string.Empty;
        public string InterfaceId
        {
            get => _interfaceId;
            set => SetProperty(ref _interfaceId, value);
        }

        private long _bytesSent;
        public long BytesSent
        {
            get => _bytesSent;
            set
            {
                if (SetProperty(ref _bytesSent, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesSent));
                }
            }
        }

        private long _bytesReceived;
        public long BytesReceived
        {
            get => _bytesReceived;
            set
            {
                if (SetProperty(ref _bytesReceived, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesReceived));
                }
            }
        }

        public string DisplayBytesReceived => NetworkApplication.FormatTraffic(BytesReceived);
        public string DisplayBytesSent => NetworkApplication.FormatTraffic(BytesSent);
    }

    public partial class DomainTraffic : ObservableObject
    {
        private string _domainName = "Resolving...";
        public string DomainName
        {
            get => _domainName;
            set => SetProperty(ref _domainName, value);
        }

        private string _remoteIp = string.Empty;
        public string RemoteIp
        {
            get => _remoteIp;
            set => SetProperty(ref _remoteIp, value);
        }

        private long _bytesSent;
        public long BytesSent
        {
            get => _bytesSent;
            set
            {
                if (SetProperty(ref _bytesSent, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesSent));
                }
            }
        }

        private long _bytesReceived;
        public long BytesReceived
        {
            get => _bytesReceived;
            set
            {
                if (SetProperty(ref _bytesReceived, value))
                {
                    OnPropertyChanged(nameof(DisplayBytesReceived));
                }
            }
        }

        public string DisplayBytesReceived => NetworkApplication.FormatTraffic(BytesReceived);
        public string DisplayBytesSent => NetworkApplication.FormatTraffic(BytesSent);
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
