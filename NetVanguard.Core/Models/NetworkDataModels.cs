using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace NetVanguard.Core.Models
{
    public class NetworkProfile : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        public string Id { get => _id; set => SetProperty(ref _id, value); }

        private string _name = string.Empty;
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public NetworkInterfaceType InterfaceType { get; set; }
        
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
        
        public long? QuotaLimitBytes { get; set; }
        public bool IsQuotaExceeded => 
            QuotaLimitBytes.HasValue && (TotalBytesSent + TotalBytesReceived) >= QuotaLimitBytes.Value;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class NetworkApplication : INotifyPropertyChanged
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public bool IsWindowsService { get; set; }
        
        private long _bytesSent;
        public long BytesSent { get => _bytesSent; set => SetProperty(ref _bytesSent, value); }

        private long _bytesReceived;
        public long BytesReceived { get => _bytesReceived; set => SetProperty(ref _bytesReceived, value); }

        public DateTime LastSeen { get; set; } = DateTime.Now;

        public bool IsBlocked { get; set; }
        public long? BandwidthLimitKbps { get; set; }

        public string DisplayBytesReceived => FormatBytes(BytesReceived);
        public string DisplayBytesSent => FormatBytes(BytesSent);

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(BytesReceived)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayBytesReceived)));
            if (name == nameof(BytesSent)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayBytesSent)));
        }
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
