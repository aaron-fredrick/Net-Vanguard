using NetVanguard.Core.Models;

namespace NetVanguard.Daemon.Services
{
    public interface IPipeServerService
    {
        Task BroadcastUpdateAsync(TrafficUpdateMessage message, CancellationToken token);
    }
}
