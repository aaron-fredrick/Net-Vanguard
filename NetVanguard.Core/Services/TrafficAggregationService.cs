using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public class TrafficAggregationService : ITrafficAggregationService, IDisposable
    {
        private readonly IEtwMonitorService _etwMonitor;
        private readonly IProcessMapperService _processMapper;
        private readonly ConcurrentQueue<NetworkTrafficEventArgs> _trafficBuffer = new();
        private readonly ConcurrentDictionary<int, NetworkApplication> _trackedApplications = new();
        private readonly ConcurrentDictionary<string, AdapterTraffic> _trackedAdapters = new();
        private readonly ConcurrentDictionary<string, DomainTraffic> _trackedDomains = new();
        private readonly Dictionary<IPAddress, string> _ipToAdapterMap = new();
        
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<TrafficUpdateMessage> OnTrafficUpdated = delegate { };

        public TrafficAggregationService(IEtwMonitorService etwMonitor, IProcessMapperService processMapper)
        {
            _etwMonitor = etwMonitor;
            _processMapper = processMapper;
            _etwMonitor.OnTrafficCaptured += onTrafficCaptured;
            refreshAdapterMap();
        }

        private void refreshAdapterMap()
        {
            _ipToAdapterMap.Clear();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                
                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    _ipToAdapterMap[addr.Address] = ni.Name;
                }
            }
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
            int refreshCounter = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    
                    // Periodically refresh adapter map in case of network changes
                    if (++refreshCounter >= 30)
                    {
                        refreshAdapterMap();
                        refreshCounter = 0;
                    }

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
                has_updates = true;

                // 1. Process Aggregation
                var app = _trackedApplications.GetOrAdd(
                    traffic_event.ProcessId,
                    pid => _processMapper.GetOrResolveApplication(pid));

                if (traffic_event.IsReceive)
                    app.BytesReceived += traffic_event.Size;
                else
                    app.BytesSent += traffic_event.Size;

                // 2. Adapter Aggregation
                string? adapterName = null;
                if (IPAddress.TryParse(traffic_event.IsReceive ? traffic_event.DestinationIp : traffic_event.SourceIp, out var localIp))
                {
                    _ipToAdapterMap.TryGetValue(localIp, out adapterName);
                }

                if (adapterName != null)
                {
                    var adapter = _trackedAdapters.GetOrAdd(adapterName, name => new AdapterTraffic { Name = name });
                    if (traffic_event.IsReceive) adapter.BytesReceived += traffic_event.Size;
                    else adapter.BytesSent += traffic_event.Size;
                }

                // 3. Domain/Remote IP Aggregation
                string remoteIp = traffic_event.IsReceive ? traffic_event.SourceIp : traffic_event.DestinationIp;
                if (!string.IsNullOrEmpty(remoteIp))
                {
                    var domain = _trackedDomains.GetOrAdd(remoteIp, ip => {
                        var d = new DomainTraffic { RemoteIp = ip };
                        // Try resolve DNS in background
                        Task.Run(async () => {
                            try {
                                var host = await Dns.GetHostEntryAsync(ip);
                                d.DomainName = host.HostName;
                            } catch { 
                                d.DomainName = ip; // Fallback to IP
                            }
                        });
                        return d;
                    });
                    
                    if (traffic_event.IsReceive) domain.BytesReceived += traffic_event.Size;
                    else domain.BytesSent += traffic_event.Size;
                }
            }

            if (has_updates)
            {
                var message = new TrafficUpdateMessage
                {
                    Applications = _trackedApplications.Values.ToList(),
                    Adapters = _trackedAdapters.Values.ToList(),
                    Domains = _trackedDomains.Values.ToList()
                };
                OnTrafficUpdated.Invoke(this, message);
            }
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
