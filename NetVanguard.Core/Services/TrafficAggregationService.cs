using System.Collections.Concurrent;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public class TrafficAggregationService : ITrafficAggregationService, IDisposable
    {
        private readonly IEtwMonitorService _etwMonitor;
        private readonly IProcessMapperService _processMapper;
        private readonly ConcurrentQueue<NetworkTrafficEventArgs> _trafficBuffer = new();
        private readonly ConcurrentDictionary<int, NetworkApplication> _trackedApplications = new();
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<IEnumerable<NetworkApplication>> OnApplicationsUpdated = delegate { };

        public TrafficAggregationService(IEtwMonitorService etwMonitor, IProcessMapperService processMapper)
        {
            _etwMonitor = etwMonitor;
            _processMapper = processMapper;
            _etwMonitor.OnTrafficCaptured += onTrafficCaptured;
        }

        public void StartAggregating()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _etwMonitor.StartMonitoring();
            Task.Run(() => runAggregationLoop(_cancellationTokenSource.Token));
        }

        public void StopAggregating()
        {
            _cancellationTokenSource?.Cancel();
            _etwMonitor.StopMonitoring();
        }

        private void onTrafficCaptured(object? sender, NetworkTrafficEventArgs e)
        {
            _trafficBuffer.Enqueue(e);
        }

        private async Task runAggregationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    drainBufferAndPublish();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void drainBufferAndPublish()
        {
            bool has_updates = false;

            while (_trafficBuffer.TryDequeue(out var traffic_event))
            {
                var app = _trackedApplications.GetOrAdd(
                    traffic_event.ProcessId,
                    pid => _processMapper.GetOrResolveApplication(pid));

                if (traffic_event.IsReceive)
                    app.BytesReceived += traffic_event.Size;
                else
                    app.BytesSent += traffic_event.Size;

                has_updates = true;
            }

            if (has_updates)
                OnApplicationsUpdated.Invoke(this, _trackedApplications.Values.ToList());
        }

        public void Dispose()
        {
            _etwMonitor.OnTrafficCaptured -= onTrafficCaptured;
            StopAggregating();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
