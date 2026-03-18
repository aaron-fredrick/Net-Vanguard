using System.Threading;
using System.Threading.Tasks;

namespace NetVanguard.Daemon.Services
{
    public interface ICommandServerService
    {
        Task StartListeningAsync(CancellationToken token);
    }
}
