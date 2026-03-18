using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface IStatisticsService
    {
        void UpdateProcessStats(string processName, long bytesSent, long bytesReceived, long maxBpsSent, long maxBpsReceived, long blockedBytes);
        void UpdateDomainStats(string remoteAddress, long bytesSent, long bytesReceived, long maxBpsSent, long maxBpsReceived, long blockedBytes);
        
        (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked) GetProcessLifetimeStats(string processName);
        (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked) GetDomainLifetimeStats(string remoteAddress);
        
        void Save();
    }
}
