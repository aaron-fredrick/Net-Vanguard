using NetVanguard.Core.Services;
using NetVanguard.Daemon;

var builder = Host.CreateApplicationBuilder(args);

// Register Core Services
builder.Services.AddSingleton<IEtwMonitorService, EtwMonitorService>();
builder.Services.AddSingleton<IProcessMapperService, ProcessMapperService>();
builder.Services.AddSingleton<ITrafficAggregationService, TrafficAggregationService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
