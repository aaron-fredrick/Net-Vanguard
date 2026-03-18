using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface ITrafficAggregationService
    {
        event EventHandler<TrafficUpdateMessage> OnTrafficUpdated;
        void StartAggregating();
        void StopAggregating();
        void SetLimit(TrafficLimitConfiguration config);
        IEnumerable<TrafficLimitConfiguration> GetLimits();
        void DeleteLimit(LimitTargetType type, string targetName);
    }
}
