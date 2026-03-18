using NetVanguard.Core.Models;
using NetVanguard.Core.Services;
using NetVanguard.Daemon.Services;

namespace NetVanguard.Daemon;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITrafficAggregationService _trafficAggregationService;
    private readonly IPipeServerService _pipeServer;

    public Worker(
        ILogger<Worker> logger, 
        ITrafficAggregationService trafficAggregationService,
        IPipeServerService pipeServer)
    {
        _logger = logger;
        _trafficAggregationService = trafficAggregationService;
        _pipeServer = pipeServer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Net-Vanguard ETW Network Monitor...");
        
        try
        {
            _trafficAggregationService.OnTrafficUpdated += async (s, message) =>
            {
                await _pipeServer.BroadcastUpdateAsync(message, stoppingToken);
                
                _logger.LogInformation($"Broadcasted update: {message.Applications.Count} apps, {message.Adapters.Count} adapters, {message.Domains.Count} domains.");
            };

            _trafficAggregationService.StartAggregating();

            await Task.Delay(-1, stoppingToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Failed to start ETW Monitor. Admin privileges required.");
            throw;
        }
        finally
        {
            _trafficAggregationService.StopAggregating();
        }
    }
}
