using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface ITrafficAggregationService
    {
        event EventHandler<TrafficUpdateMessage> OnTrafficUpdated;
        void StartAggregating();
        void StopAggregating();
    }
}
