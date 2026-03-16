using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public class EtwMonitorService : IEtwMonitorService
    {
        private const string EtwSessionName = "NetVanguard_NetworkMonitorSession";
        private TraceEventSession? _session;

        public event EventHandler<NetworkTrafficEventArgs> OnTrafficCaptured = delegate { };

        public void StartMonitoring()
        {
            Task.Run(runEtwSession);
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

        private void runEtwSession()
        {
            if (!(TraceEventSession.IsElevated() ?? false))
                throw new UnauthorizedAccessException("ETW Monitoring requires Administrator privileges.");

            using (_session = new TraceEventSession(EtwSessionName))
            {
                _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                _session.Source.Kernel.TcpIpRecv += data => raiseTrafficEvent("TCP", isReceive: true, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);
                _session.Source.Kernel.TcpIpSend += data => raiseTrafficEvent("TCP", isReceive: false, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);
                _session.Source.Kernel.UdpIpRecv += data => raiseTrafficEvent("UDP", isReceive: true, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);
                _session.Source.Kernel.UdpIpSend += data => raiseTrafficEvent("UDP", isReceive: false, data.ProcessID, data.size, data.saddr.ToString(), data.daddr.ToString(), data.sport, data.dport);

                _session.Source.Process();
            }
        }

        private void raiseTrafficEvent(string protocol, bool isReceive, int processId, int size, string srcIp, string destIp, int srcPort, int destPort)
        {
            OnTrafficCaptured.Invoke(this, new NetworkTrafficEventArgs
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
    }
}
