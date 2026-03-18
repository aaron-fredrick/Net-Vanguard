using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetVanguard.Core.Infrastructure;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Services
{
    public class CommandClientService : ICommandClientService
    {
        private async Task<CommandResponse?> SendCommandAsync(CommandMessage request, CancellationToken token)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeConstants.CommandPipeName, PipeDirection.InOut);
                await client.ConnectAsync(1000, token); // Wait max 1 second for daemon
                
                using var reader = new StreamReader(client, leaveOpen: true);
                using var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true };

                var jsonRequest = JsonSerializer.Serialize(request);
                await writer.WriteLineAsync(jsonRequest);

                var jsonResponse = await reader.ReadLineAsync(token);
                if (!string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return JsonSerializer.Deserialize<CommandResponse>(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Command Pipe Error] {ex.Message}");
            }
            return null;
        }

        public async Task<IEnumerable<FirewallRuleModel>> GetAllRulesAsync(CancellationToken token = default)
        {
            var response = await SendCommandAsync(new CommandMessage { Command = CommandType.GetAllRules }, token);
            if (response?.Success == true && !string.IsNullOrWhiteSpace(response.Payload))
            {
                var rules = JsonSerializer.Deserialize<List<FirewallRuleModel>>(response.Payload);
                if (rules != null) return rules;
            }
            return new List<FirewallRuleModel>();
        }

        public async Task<bool> AddRuleAsync(FirewallRuleModel rule, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(rule);
            var response = await SendCommandAsync(new CommandMessage { Command = CommandType.AddRule, Payload = payload }, token);
            return response?.Success ?? false;
        }

        public async Task<bool> SetRuleEnabledAsync(string ruleName, bool enabled, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new { Name = ruleName, Enabled = enabled });
            var response = await SendCommandAsync(new CommandMessage { Command = CommandType.SetRuleEnabled, Payload = payload }, token);
            return response?.Success ?? false;
        }

        public async Task<bool> DeleteRuleAsync(string ruleName, CancellationToken token = default)
        {
            var response = await SendCommandAsync(new CommandMessage { Command = CommandType.DeleteRule, Payload = ruleName }, token);
            return response?.Success ?? false;
        }
    }
}
