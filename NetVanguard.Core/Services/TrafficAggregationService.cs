using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface ITrafficAggregationService
    {
        event EventHandler<IEnumerable<NetworkApplication>> OnApplicationsUpdated;
        void StartAggregating();
        void StopAggregating();
    }

    public class TrafficAggregationService : ITrafficAggregationService, IDisposable
    {
        private readonly IEtwMonitorService _etwMonitor;
        private readonly IProcessMapperService _processMapper;
        private readonly ConcurrentQueue<NetworkTrafficEventArgs> _trafficBuffer = new();
        private CancellationTokenSource? _cancellationTokenSource;

        // In-memory state of all tracked applications
        private readonly ConcurrentDictionary<int, NetworkApplication> _trackedApplications = new();

        public event EventHandler<IEnumerable<NetworkApplication>> OnApplicationsUpdated = delegate { };

        public TrafficAggregationService(IEtwMonitorService etwMonitor, IProcessMapperService processMapper)
        {
            _etwMonitor = etwMonitor;
            _processMapper = processMapper;
            
            _etwMonitor.OnTrafficCaptured += EtwMonitor_OnTrafficCaptured;
        }

        private void EtwMonitor_OnTrafficCaptured(object? sender, NetworkTrafficEventArgs e)
        {
            _trafficBuffer.Enqueue(e);
        }

        public void StartAggregating()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _etwMonitor.StartMonitoring();
            
            Task.Run(() => AggregationLoop(_cancellationTokenSource.Token));
        }

        private async Task AggregationLoop(CancellationToken token)
        {
            // Process the buffer every second
            var interval = TimeSpan.FromSeconds(1);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, token);
                    ProcessBuffer();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void ProcessBuffer()
        {
            bool hasUpdates = false;

            while (_trafficBuffer.TryDequeue(out var trafficEvent))
            {
                var app = _trackedApplications.GetOrAdd(trafficEvent.ProcessId, pid => 
                    _processMapper.GetOrResolveApplication(pid));

                if (trafficEvent.IsReceive)
                {
                    app.BytesReceived += trafficEvent.Size;
                }
                else
                {
                    app.BytesSent += trafficEvent.Size;
                }
                
                hasUpdates = true;
            }

            if (hasUpdates)
            {
                // Broadcast a snapshot of the current state
                OnApplicationsUpdated?.Invoke(this, _trackedApplications.Values.ToList());
            }
        }

        public void StopAggregating()
        {
            _cancellationTokenSource?.Cancel();
            _etwMonitor.StopMonitoring();
        }

        public void Dispose()
        {
            _etwMonitor.OnTrafficCaptured -= EtwMonitor_OnTrafficCaptured;
            StopAggregating();
            _cancellationTokenSource?.Dispose();
        }
    }
}
