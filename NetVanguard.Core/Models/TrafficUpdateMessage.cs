using System.Collections.Generic;

namespace NetVanguard.Core.Models
{
    public class TrafficUpdateMessage
    {
        public List<NetworkApplication> Applications { get; set; } = new();
        public long Timestamp { get; set; } = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
