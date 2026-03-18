using NetVanguard.Core.Services;
using NetVanguard.Daemon;
using NetVanguard.Daemon.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register Core Services
builder.Services.AddSingleton<IEtwMonitorService, EtwMonitorService>();
builder.Services.AddSingleton<IProcessMapperService, ProcessMapperService>();
builder.Services.AddSingleton<ITrafficAggregationService, TrafficAggregationService>();
builder.Services.AddSingleton<IPipeServerService, PipeServerService>();
builder.Services.AddSingleton<IFirewallManagerService, WindowsFirewallService>();
builder.Services.AddSingleton<ICommandServerService, CommandServerService>();
builder.Services.AddSingleton<IQosThrottlingService, QosThrottlingService>();

builder.Services.AddHostedService<QuotaTrackingEngine>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
