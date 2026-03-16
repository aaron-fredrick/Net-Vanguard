using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetVanguard.Core.Infrastructure;
using NetVanguard.Core.Models;

namespace NetVanguard.Daemon.Services
{
    public class PipeServerService : IPipeServerService
    {
        private readonly ILogger<PipeServerService> _logger;

        public PipeServerService(ILogger<PipeServerService> logger)
        {
            _logger = logger;
        }

        public async Task BroadcastUpdateAsync(TrafficUpdateMessage message, CancellationToken token)
        {
            try
            {
                using var pipe_server = new NamedPipeServerStream(
                    PipeConstants.TrafficPipeName,
                    PipeDirection.Out,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                using var timeout_cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout_cts.CancelAfter(TimeSpan.FromMilliseconds(500));

                await pipe_server.WaitForConnectionAsync(timeout_cts.Token);
                await JsonSerializer.SerializeAsync(pipe_server, message, cancellationToken: token);
                await pipe_server.FlushAsync(token);
            }
            catch (OperationCanceledException)
            {
                // No client connected within the timeout window; skip this broadcast cycle.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pipe broadcast failed.");
            }
        }
    }
}
