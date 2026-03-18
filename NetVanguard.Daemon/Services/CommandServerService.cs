using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetVanguard.Core.Infrastructure;
using NetVanguard.Core.Models;
using NetVanguard.Core.Services;

namespace NetVanguard.Daemon.Services
{
    public class CommandServerService : ICommandServerService
    {
        private readonly IFirewallManagerService _firewallService;
        private readonly ITrafficAggregationService _trafficAggregationService;
        private readonly ILogger<CommandServerService> _logger;

        public CommandServerService(IFirewallManagerService firewallService, ITrafficAggregationService trafficAggregationService, ILogger<CommandServerService> logger)
        {
            _firewallService = firewallService;
            _trafficAggregationService = trafficAggregationService;
            _logger = logger;
        }

        public async Task StartListeningAsync(CancellationToken token)
        {
            _logger.LogInformation("Command Server listening on {PipeName}", PipeConstants.CommandPipeName);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeConstants.CommandPipeName,
                        PipeDirection.InOut,
                        maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);

                    // Read request
                    using var reader = new StreamReader(server, leaveOpen: true);
                    using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };

                    var jsonRequest = await reader.ReadLineAsync(token);
                    if (!string.IsNullOrWhiteSpace(jsonRequest))
                    {
                        var request = JsonSerializer.Deserialize<CommandMessage>(jsonRequest);
                        if (request != null)
                        {
                            var response = ProcessCommand(request);
                            var jsonResponse = JsonSerializer.Serialize(response);
                            await writer.WriteLineAsync(jsonResponse);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing command pipe connection.");
                }
            }
        }

        private CommandResponse ProcessCommand(CommandMessage request)
        {
            var response = new CommandResponse { Success = true };

            try
            {
                switch (request.Command)
                {
                    case CommandType.GetAllRules:
                        var rules = _firewallService.GetAllRules();
                        response.Payload = JsonSerializer.Serialize(rules);
                        break;

                    case CommandType.AddRule:
                        if (string.IsNullOrWhiteSpace(request.Payload)) throw new ArgumentException("Payload is empty.");
                        var ruleToAdd = JsonSerializer.Deserialize<FirewallRuleModel>(request.Payload);
                        if (ruleToAdd != null) _firewallService.AddRule(ruleToAdd);
                        break;

                    case CommandType.SetRuleEnabled:
                        if (string.IsNullOrWhiteSpace(request.Payload)) throw new ArgumentException("Payload is empty.");
                        var enableCmd = JsonSerializer.Deserialize<SetRuleEnabledPayload>(request.Payload);
                        if (enableCmd != null) _firewallService.SetRuleEnabled(enableCmd.Name, enableCmd.Enabled);
                        break;

                    case CommandType.DeleteRule:
                        if (string.IsNullOrWhiteSpace(request.Payload)) throw new ArgumentException("Payload is empty.");
                        _firewallService.DeleteRule(request.Payload); // payload is directly the string name
                        break;
                        
                    case CommandType.SetLimit:
                        if (string.IsNullOrWhiteSpace(request.Payload)) throw new ArgumentException("Payload is empty.");
                        var limitCmd = JsonSerializer.Deserialize<SetLimitPayload>(request.Payload);
                        if (limitCmd != null) 
                        {
                            _trafficAggregationService.SetLimit(new TrafficLimitConfiguration 
                            { 
                                TargetType = limitCmd.TargetType, 
                                TargetName = limitCmd.TargetName, 
                                DataQuotaBytes = limitCmd.DataQuotaBytes, 
                                ThrottleLimitBps = limitCmd.ThrottleLimitBps 
                            });
                        }
                        break;
                        
                    case CommandType.GetLimits:
                        var limits = _trafficAggregationService.GetLimits();
                        response.Payload = JsonSerializer.Serialize(limits);
                        break;
                        
                    case CommandType.DeleteLimit:
                        if (string.IsNullOrWhiteSpace(request.Payload)) throw new ArgumentException("Payload is empty.");
                        var delCmd = JsonSerializer.Deserialize<DeleteLimitPayload>(request.Payload);
                        if (delCmd != null) _trafficAggregationService.DeleteLimit(delCmd.TargetType, delCmd.TargetName);
                        break;

                    default:
                        response.Success = false;
                        response.ErrorMessage = "Unknown command type.";
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to process command {Command}.", request.Command);
            }

            return response;
        }

        public class SetRuleEnabledPayload
        {
            public string Name { get; set; } = string.Empty;
            public bool Enabled { get; set; }
        }

        public class SetLimitPayload
        {
            public NetVanguard.Core.Models.LimitTargetType TargetType { get; set; }
            public string TargetName { get; set; } = string.Empty;
            public long? DataQuotaBytes { get; set; }
            public long? ThrottleLimitBps { get; set; }
        }
        
        public class DeleteLimitPayload
        {
            public NetVanguard.Core.Models.LimitTargetType TargetType { get; set; }
            public string TargetName { get; set; } = string.Empty;
        }
    }
}
