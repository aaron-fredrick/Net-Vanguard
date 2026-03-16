using System.Collections.Concurrent;
using System.Diagnostics;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public class ProcessMapperService : IProcessMapperService
    {
        private readonly ConcurrentDictionary<int, NetworkApplication> _processCache = new();

        public NetworkApplication GetOrResolveApplication(int processId)
        {
            if (processId <= 0)
                return new NetworkApplication { ProcessId = processId, ProcessName = "System", ExecutablePath = "System" };

            return _processCache.GetOrAdd(processId, resolveProcessInfo);
        }

        private static NetworkApplication resolveProcessInfo(int pid)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return new NetworkApplication
                {
                    ProcessId = pid,
                    ProcessName = process.ProcessName,
                    ExecutablePath = process.MainModule?.FileName ?? string.Empty,
                    IsWindowsService = process.MainModule?.FileName?.EndsWith("svchost.exe", StringComparison.OrdinalIgnoreCase) ?? false
                };
            }
            catch (Exception)
            {
                return new NetworkApplication
                {
                    ProcessId = pid,
                    ProcessName = $"Unknown (PID {pid})",
                    ExecutablePath = string.Empty
                };
            }
        }
    }
}
