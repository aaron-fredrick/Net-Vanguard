using NetVanguard.Core.Services;

namespace NetVanguard.Daemon;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITrafficAggregationService _trafficAggregationService;

    public Worker(ILogger<Worker> logger, ITrafficAggregationService trafficAggregationService)
    {
        _logger = logger;
        _trafficAggregationService = trafficAggregationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Net-Vanguard ETW Network Monitor...");
        
        try
        {
            _trafficAggregationService.OnApplicationsUpdated += (s, apps) =>
            {
                // In Phase 3, we will pipe this via gRPC/NamedPipes to the UI
                _logger.LogInformation($"Tracked {apps.Count()} active applications.");
            };

            _trafficAggregationService.StartAggregating();

            // Wait until cancellation is requested
            await Task.Delay(-1, stoppingToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Failed to start ETW Monitor. Did you run the application as Administrator?");
            throw;
        }
        finally
        {
            _trafficAggregationService.StopAggregating();
        }
    }
}
