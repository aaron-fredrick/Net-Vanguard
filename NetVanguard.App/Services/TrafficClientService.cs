using System;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Services
{
    public interface ITrafficClientService
    {
        event EventHandler<TrafficUpdateMessage> OnMessageReceived;
        void StartListening();
    }

    public class TrafficClientService : ITrafficClientService
    {
        private const string PipeName = "NetVanguard_TrafficPipe";
        public event EventHandler<TrafficUpdateMessage> OnMessageReceived = delegate { };

        public void StartListening()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In);
                        await pipeClient.ConnectAsync(1000);

                        if (pipeClient.IsConnected)
                        {
                            using var reader = new System.IO.StreamReader(pipeClient);
                            while (pipeClient.IsConnected)
                            {
                                // In a real app we might want a more robust framing, 
                                // but for a 1/s update, JSON-per-connection is simple.
                                var message = await JsonSerializer.DeserializeAsync<TrafficUpdateMessage>(pipeClient);
                                if (message != null)
                                {
                                    OnMessageReceived?.Invoke(this, message);
                                }
                            }
                        }
                    }
                    catch 
                    { 
                        // Server not active or connection lost, wait and retry
                        await Task.Delay(1000); 
                    }
                }
            });
        }
    }
}
