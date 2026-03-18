using Microsoft.Extensions.Hosting;
using NetVanguard.Core.Models;
using NetVanguard.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetVanguard.Daemon.Services
{
    public class QuotaTrackingEngine : IHostedService
    {
        private readonly ITrafficAggregationService _trafficService;
        private readonly IFirewallManagerService _firewallManager;
        private readonly IQosThrottlingService _qosThrottlingService;
        private readonly HashSet<string> _enforcedBlocks = new();
        private readonly HashSet<string> _enforcedThrottles = new();

        public QuotaTrackingEngine(
            ITrafficAggregationService trafficService, 
            IFirewallManagerService firewallManager,
            IQosThrottlingService qosThrottlingService)
        {
            _trafficService = trafficService;
            _firewallManager = firewallManager;
            _qosThrottlingService = qosThrottlingService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _trafficService.OnTrafficUpdated += OnTrafficUpdated;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _trafficService.OnTrafficUpdated -= OnTrafficUpdated;
            return Task.CompletedTask;
        }

        private void OnTrafficUpdated(object? sender, TrafficUpdateMessage e)
        {
            foreach (var app in e.Applications)
            {
                // 1. Check Data Quota Blocks
                if (app.DataQuotaBytes.HasValue)
                {
                    long totalBytes = app.BytesSent + app.BytesReceived;
                    if (totalBytes >= app.DataQuotaBytes.Value && !_enforcedBlocks.Contains(app.ProcessName))
                    {
                        var ruleName = $"Net-Vanguard: QuotaBlock_{app.ProcessName}";
                        var rule = new FirewallRuleModel
                        {
                            Name = ruleName,
                            ApplicationName = app.ExecutablePath,
                            Action = FirewallAction.Block,
                            Direction = FirewallDirection.Outbound,
                            Enabled = true
                        };
                        
                        _firewallManager.AddRule(rule);
                        _enforcedBlocks.Add(app.ProcessName);
                        Console.WriteLine($"[QUOTA ENGINE] {app.ProcessName} exceeded {app.DataQuotaBytes} limit. Hard Firewall block injected natively.");
                    }
                }

                // 2. Check Static QoS Throttling Rules
                if (app.ThrottleLimitBps.HasValue && !_enforcedThrottles.Contains(app.ProcessName))
                {
                    _qosThrottlingService.ApplyThrottleRule(app.ProcessName, app.ThrottleLimitBps.Value);
                    _enforcedThrottles.Add(app.ProcessName);
                }
                else if (!app.ThrottleLimitBps.HasValue && _enforcedThrottles.Contains(app.ProcessName))
                {
                    _qosThrottlingService.RemoveThrottleRule(app.ProcessName);
                    _enforcedThrottles.Remove(app.ProcessName);
                }
            }
        }
    }
}
