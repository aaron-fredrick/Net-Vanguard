using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetVanguard.Core.Models;

namespace NetVanguard.Daemon.Services
{
    public interface IPipeServerService
    {
        Task BroadcastUpdateAsync(TrafficUpdateMessage message, CancellationToken token);
    }

    public class PipeServerService : IPipeServerService
    {
        private const string PipeName = "NetVanguard_TrafficPipe";
        private readonly ILogger<PipeServerService> _logger;

        public PipeServerService(ILogger<PipeServerService> logger)
        {
            _logger = logger;
        }

        public async Task BroadcastUpdateAsync(TrafficUpdateMessage message, CancellationToken token)
        {
            try
            {
                // Note: Standard WinUI apps aren't allowed to connect to pipes created by Admin
                // unless we specify a Security Descriptor. For now, we'll use a simple one.
                using var pipeServer = new NamedPipeServerStream(
                    PipeName, 
                    PipeDirection.Out, 
                    1, 
                    PipeTransmissionMode.Byte, 
                    PipeOptions.Asynchronous);
                
                // Wait for the app to connect with a short timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(500); 

                await pipeServer.WaitForConnectionAsync(cts.Token);

                if (pipeServer.IsConnected)
                {
                    await JsonSerializer.SerializeAsync(pipeServer, message, cancellationToken: token);
                    await pipeServer.FlushAsync(token);
                }
            }
            catch (OperationCanceledException) { /* No client connected, skip this tick */ }
            catch (Exception ex)
            {
                _logger.LogWarning($"Pipe broadcast failed: {ex.Message}");
            }
        }
    }
}
