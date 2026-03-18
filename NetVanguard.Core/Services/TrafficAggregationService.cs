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
        private readonly ConcurrentDictionary<string, TrafficLimitConfiguration> _activeLimits = new();
        private readonly IStatisticsService _statsService;
        
        private string GetLimitKey(LimitTargetType type, string targetName) => $"{type}:{targetName.ToLowerInvariant()}";
        
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<TrafficUpdateMessage> OnTrafficUpdated = delegate { };

        public TrafficAggregationService(IEtwMonitorService etwMonitor, IProcessMapperService processMapper, IStatisticsService statsService)
        {
            _etwMonitor = etwMonitor;
            _processMapper = processMapper;
            _statsService = statsService;
            _etwMonitor.OnTrafficCaptured += OnTrafficCaptured;
            RefreshAdapterMap();
        }

        private void RefreshAdapterMap()
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
            Task.Run(() => RunAggregationLoop(_cancellationTokenSource.Token));
        }

        public void StopAggregating()
        {
            _cancellationTokenSource?.Cancel();
            _etwMonitor.StopMonitoring();
        }

        public void SetLimit(TrafficLimitConfiguration config)
        {
            var key = GetLimitKey(config.TargetType, config.TargetName);
            _activeLimits[key] = config;

            if (config.TargetType == LimitTargetType.Process)
            {
                foreach (var application in _trackedApplications.Values)
                {
                    if (string.Equals(application.ProcessName, config.TargetName, StringComparison.OrdinalIgnoreCase))
                    {
                        application.DataQuotaBytes = config.DataQuotaBytes;
                        application.ThrottleLimitBps = config.ThrottleLimitBps;
                    }
                }
            }
        }

        public IEnumerable<TrafficLimitConfiguration> GetLimits() => _activeLimits.Values;

        public void DeleteLimit(LimitTargetType type, string targetName)
        {
            _activeLimits.TryRemove(GetLimitKey(type, targetName), out _);
            if (type == LimitTargetType.Process)
            {
                foreach (var application in _trackedApplications.Values)
                {
                    if (string.Equals(application.ProcessName, targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        application.DataQuotaBytes = null;
                        application.ThrottleLimitBps = null;
                    }
                }
            }
        }

        private void OnTrafficCaptured(object? sender, NetworkTrafficEventArgs e)
        {
            _trafficBuffer.Enqueue(e);
        }

        private async Task RunAggregationLoop(CancellationToken token)
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
                        RefreshAdapterMap();
                        refreshCounter = 0;
                    }

                    DrainBufferAndPublish();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void DrainBufferAndPublish()
        {
            bool hasUpdates = false;

            var appDeltas = new Dictionary<string, (long Sent, long Recv)>();
            var domainDeltas = new Dictionary<string, (long Sent, long Recv)>();

            while (_trafficBuffer.TryDequeue(out var trafficEvent))
            {
                hasUpdates = true;

                // 1. Process Aggregation
                var app = _trackedApplications.GetOrAdd(
                    trafficEvent.ProcessId,
                    pid => 
                    {
                        var newApp = _processMapper.GetOrResolveApplication(pid);
                        if (_activeLimits.TryGetValue(GetLimitKey(LimitTargetType.Process, newApp.ProcessName), out var limit))
                        {
                            newApp.DataQuotaBytes = limit.DataQuotaBytes;
                            newApp.ThrottleLimitBps = limit.ThrottleLimitBps;
                        }
                        return newApp;
                    });

                long size = trafficEvent.Size;
                if (trafficEvent.IsReceive) app.BytesReceived += size;
                else app.BytesSent += size;

                // Track 1s delta for BPS/Peak detection
                if (!appDeltas.ContainsKey(app.ProcessName)) appDeltas[app.ProcessName] = (0, 0);
                var ad = appDeltas[app.ProcessName];
                appDeltas[app.ProcessName] = trafficEvent.IsReceive ? (ad.Sent, ad.Recv + size) : (ad.Sent + size, ad.Recv);

                // 2. Adapter Aggregation
                string? adapterName = null;
                if (IPAddress.TryParse(trafficEvent.IsReceive ? trafficEvent.DestinationIp : trafficEvent.SourceIp, out var localIp))
                {
                    _ipToAdapterMap.TryGetValue(localIp, out adapterName);
                }

                if (adapterName != null)
                {
                    var adapter = _trackedAdapters.GetOrAdd(adapterName, name => new AdapterTraffic { Name = name });
                    if (trafficEvent.IsReceive) adapter.BytesReceived += size;
                    else adapter.BytesSent += size;
                }

                // 3. Domain/Remote IP Aggregation
                string remoteIp = trafficEvent.IsReceive ? trafficEvent.SourceIp : trafficEvent.DestinationIp;
                if (!string.IsNullOrEmpty(remoteIp))
                {
                    var domain = _trackedDomains.GetOrAdd(remoteIp, ip => {
                        var d = new DomainTraffic { RemoteIp = ip };
                        Task.Run(async () => {
                            try {
                                var host = await Dns.GetHostEntryAsync(ip);
                                d.DomainName = host.HostName;
                                // Capture reversed DNS records
                                d.DnsRecords.Clear();
                                if (host.Aliases.Length > 0) d.DnsRecords.AddRange(host.Aliases);
                                foreach (var address in host.AddressList) d.DnsRecords.Add($"IP: {address}");
                            } catch { 
                                d.DomainName = ip;
                            }
                        });
                        return d;
                    });
                    
                    if (trafficEvent.IsReceive) domain.BytesReceived += size;
                    else domain.BytesSent += size;

                    if (!domainDeltas.ContainsKey(domain.RemoteIp)) domainDeltas[domain.RemoteIp] = (0, 0);
                    var dd = domainDeltas[domain.RemoteIp];
                    domainDeltas[domain.RemoteIp] = trafficEvent.IsReceive ? (dd.Sent, dd.Recv + size) : (dd.Sent + size, dd.Recv);

                    // Cross-Relational Linkage
                    string linkId = domain.DomainName != "Resolving..." ? domain.DomainName : domain.RemoteIp;
                    if (!app.ConnectedDomains.Contains(linkId)) app.ConnectedDomains.Add(linkId);
                    if (!domain.EngagingProcesses.Contains(app.ProcessName)) domain.EngagingProcesses.Add(app.ProcessName);
                }
            }

            if (hasUpdates)
            {
                // Push deltas to persistence for totals and peak tracking
                foreach (var kvp in appDeltas)
                    _statsService.UpdateProcessStats(kvp.Key, kvp.Value.Sent, kvp.Value.Recv, kvp.Value.Sent, kvp.Value.Recv, 0); // TODO: Add blocked telemetry
                
                foreach (var kvp in domainDeltas)
                    _statsService.UpdateDomainStats(kvp.Key, kvp.Value.Sent, kvp.Value.Recv, kvp.Value.Sent, kvp.Value.Recv, 0);

                // Assign Lifetime Stats before publishing
                foreach (var app in _trackedApplications.Values)
                {
                    var (ts, tr, ms, mr, tb) = _statsService.GetProcessLifetimeStats(app.ProcessName);
                    app.LifetimeTotalBytesSent = ts;
                    app.LifetimeTotalBytesReceived = tr;
                    app.LifetimeMaxBytesSent = ms;
                    app.LifetimeMaxBytesReceived = mr;
                    app.LifetimeTotalBytesBlocked = tb;
                }

                foreach (var domain in _trackedDomains.Values)
                {
                    var (ts, tr, ms, mr, tb) = _statsService.GetDomainLifetimeStats(domain.RemoteIp);
                    domain.LifetimeTotalBytesSent = ts;
                    domain.LifetimeTotalBytesReceived = tr;
                    domain.LifetimeMaxBytesSent = ms;
                    domain.LifetimeMaxBytesReceived = mr;
                    domain.LifetimeTotalBytesBlocked = tb;
                }

                var message = new TrafficUpdateMessage
                {
                    Applications = _trackedApplications.Values.ToList(),
                    Adapters = _trackedAdapters.Values.ToList(),
                    Domains = _trackedDomains.Values.ToList()
                };
                OnTrafficUpdated.Invoke(this, message);
                
                // Periodic DB Save
                _statsService.Save();
            }
        }

        public void Dispose()
        {
            _etwMonitor.OnTrafficCaptured -= OnTrafficCaptured;
            StopAggregating();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
