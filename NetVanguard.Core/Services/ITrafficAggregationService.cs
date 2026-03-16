using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface ITrafficAggregationService
    {
        event EventHandler<IEnumerable<NetworkApplication>> OnApplicationsUpdated;
        void StartAggregating();
        void StopAggregating();
    }
}
