using System.IO.Pipes;
using System.Text.Json;
using NetVanguard.Core.Infrastructure;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Services
{
    public class TrafficClientService : ITrafficClientService
    {
        public event EventHandler<TrafficUpdateMessage> OnMessageReceived = delegate { };

        public void StartListening(CancellationToken token = default)
        {
            Task.Run(() => runListenLoop(token), token);
        }

        private async Task runListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var pipe_client = new NamedPipeClientStream(".", PipeConstants.TrafficPipeName, PipeDirection.In);
                    await pipe_client.ConnectAsync(1000, token);

                    var message = await JsonSerializer.DeserializeAsync<TrafficUpdateMessage>(pipe_client, cancellationToken: token);
                    if (message is not null)
                        OnMessageReceived.Invoke(this, message);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Daemon not yet running or connection dropped; retry after a brief delay.
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
            }
        }
    }
}
