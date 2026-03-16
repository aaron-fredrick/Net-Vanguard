using System;
using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface IEtwMonitorService : IDisposable
    {
        event EventHandler<NetworkTrafficEventArgs> OnTrafficCaptured;
        void StartMonitoring();
        void StopMonitoring();
    }
}
