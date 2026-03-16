using System;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public class EtwMonitorService : IEtwMonitorService
    {
        // Must be unique system-wide
        private const string SessionName = "NetVanguard_NetworkMonitorSession";
        private TraceEventSession? _session;

        public event EventHandler<NetworkTrafficEventArgs> OnTrafficCaptured = delegate { };

        public void StartMonitoring()
        {
            Task.Run(() =>
            {
                // ETW Kernel traces require Administrator privileges
                if (!(TraceEventSession.IsElevated() ?? false))
                {
                    throw new UnauthorizedAccessException("ETW Monitoring requires Administrator privileges. Please restart the background service as Admin.");
                }

                // Initialize the Trace Event Session
                using (_session = new TraceEventSession(SessionName))
                {
                    // Enable exclusively Network events to minimize overhead
                    _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    // Subscribe to TCP events
                    _session.Source.Kernel.TcpIpRecv += data => ProcessEvent("TCP", true, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);
                    _session.Source.Kernel.TcpIpSend += data => ProcessEvent("TCP", false, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);

                    // Subscribe to UDP events
                    _session.Source.Kernel.UdpIpRecv += data => ProcessEvent("UDP", true, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);
                    _session.Source.Kernel.UdpIpSend += data => ProcessEvent("UDP", false, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);

                    // This is a blocking call that continuously processes the ETW stream
                    _session.Source.Process();
                }
            });
        }

        private void ProcessEvent(string protocol, bool isReceive, int processId, int size, string srcIp, string destIp, int srcPort, int destPort)
        {
            OnTrafficCaptured?.Invoke(this, new NetworkTrafficEventArgs
            {
                Protocol = protocol,
                IsReceive = isReceive,
                ProcessId = processId,
                Size = size,
                SourceIp = srcIp,
                DestinationIp = destIp,
                SourcePort = srcPort,
                DestinationPort = destPort
            });
        }

        public void StopMonitoring()
        {
            _session?.Dispose();
        }

        public void Dispose()
        {
            StopMonitoring();
            GC.SuppressFinalize(this);
        }
    }
}
