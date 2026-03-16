using System;

namespace NetVanguard.Core.Models
{
    public class NetworkTrafficEventArgs : EventArgs
    {
        public int ProcessId { get; set; }
        public int Size { get; set; } // Bytes transferred
        public bool IsReceive { get; set; } // True for Download, False for Upload
        public string SourceIp { get; set; } = string.Empty;
        public string DestinationIp { get; set; } = string.Empty;
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public string Protocol { get; set; } = string.Empty; // "TCP" or "UDP"
    }
}
