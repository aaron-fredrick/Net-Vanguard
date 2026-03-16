using NetVanguard.Core.Models;

namespace NetVanguard.App.Services
{
    public interface ITrafficClientService
    {
        event EventHandler<TrafficUpdateMessage> OnMessageReceived;
        void StartListening(CancellationToken token = default);
    }
}
