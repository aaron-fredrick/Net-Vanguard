using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface IProcessMapperService
    {
        NetworkApplication GetOrResolveApplication(int processId);
    }

    public class ProcessMapperService : IProcessMapperService
    {
        private readonly ConcurrentDictionary<int, NetworkApplication> _processCache = new();

        public NetworkApplication GetOrResolveApplication(int processId)
        {
            if (processId <= 0)
            {
                return new NetworkApplication { ProcessId = processId, ProcessName = "System", ExecutablePath = "System" };
            }

            return _processCache.GetOrAdd(processId, pid => ResolveProcessInfo(pid));
        }

        private NetworkApplication ResolveProcessInfo(int pid)
        {
            var app = new NetworkApplication { ProcessId = pid };
            try
            {
                using var process = Process.GetProcessById(pid);
                app.ProcessName = process.ProcessName;
                
                // Getting MainModule.FileName can throw AccessDenied for elevated processes
                // if NetVanguard isn't running as admin, but Daemon will be admin.
                app.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                
                // Heuristic for services
                app.IsWindowsService = app.ExecutablePath.EndsWith("svchost.exe", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Process might have exited already or Access Denied
                app.ProcessName = $"Unknown (PID {pid})";
                app.ExecutablePath = string.Empty;
            }

            return app;
        }
    }
}
