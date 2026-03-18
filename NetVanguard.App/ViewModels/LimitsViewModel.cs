using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NetVanguard.Core.Infrastructure;
using NetVanguard.Core.Models;

namespace NetVanguard.App.ViewModels
{
    public partial class LimitsViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailPaneVisible))]
        [NotifyPropertyChangedFor(nameof(IsDetailPaneHidden))]
        [NotifyPropertyChangedFor(nameof(DetailTargetName))]
        [NotifyPropertyChangedFor(nameof(DetailTargetType))]
        [NotifyPropertyChangedFor(nameof(DetailTotalBytesBlocked))]
        [NotifyPropertyChangedFor(nameof(DetailDataQuota))]
        [NotifyPropertyChangedFor(nameof(DetailThrottle))]
        public partial TrafficLimitConfiguration? SelectedLimit { get; set; }

        public bool IsDetailPaneVisible => SelectedLimit != null;
        public bool IsDetailPaneHidden => SelectedLimit == null;

        public string DetailTargetName => SelectedLimit?.TargetName ?? string.Empty;
        public string DetailTargetType => SelectedLimit?.TargetType.ToString() ?? string.Empty;
        public string DetailTotalBytesBlocked => SelectedLimit != null ? NetworkApplication.FormatTraffic(SelectedLimit.TotalBytesBlocked) : "0 B";
        public string DetailDataQuota => SelectedLimit?.DataQuotaBytes != null ? NetworkApplication.FormatTraffic(SelectedLimit.DataQuotaBytes.Value) : "No Quota";
        public string DetailThrottle => SelectedLimit?.ThrottleLimitBps != null ? $"{NetworkApplication.FormatTraffic(SelectedLimit.ThrottleLimitBps.Value)}/s" : "Unrestricted";

        public ObservableCollection<TrafficLimitConfiguration> ActiveLimits { get; } = new();

        public LimitsViewModel()
        {
            FetchLimitsCommand(); // Load immediately
        }

        public async void TransmitSetLimitCommand(LimitTargetType type, string targetName, long? quotaBytes, long? throttleBps)
        {
            try
            {
                var payload = new 
                { 
                    TargetType = type,
                    TargetName = targetName, 
                    DataQuotaBytes = quotaBytes, 
                    ThrottleLimitBps = throttleBps 
                };

                var cmd = new CommandMessage
                {
                    Command = CommandType.SetLimit,
                    Payload = JsonSerializer.Serialize(payload)
                };

                await sendCommand(cmd);
                await Task.Delay(200); // Give Daemon time to process
                FetchLimitsCommand(); // Refresh local array
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending SetLimit: {ex}");
            }
        }

        public async void TransmitDeleteLimitCommand(LimitTargetType type, string targetName)
        {
            try
            {
                var payload = new { TargetType = type, TargetName = targetName };
                var cmd = new CommandMessage { Command = CommandType.DeleteLimit, Payload = JsonSerializer.Serialize(payload) };
                await sendCommand(cmd);
                
                // Clear selection cleanly
                SelectedLimit = null;
                
                await Task.Delay(200);
                FetchLimitsCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending DeleteLimit: {ex}");
            }
        }

        public async void FetchLimitsCommand()
        {
            try
            {
                var cmd = new CommandMessage { Command = CommandType.GetLimits };
                var jsonResponse = await sendCommand(cmd);

                if (!string.IsNullOrWhiteSpace(jsonResponse))
                {
                    var response = JsonSerializer.Deserialize<CommandResponse>(jsonResponse);
                    if (response != null && response.Success && !string.IsNullOrWhiteSpace(response.Payload))
                    {
                        var limitsList = JsonSerializer.Deserialize<TrafficLimitConfiguration[]>(response.Payload);
                        if (limitsList != null)
                        {
                            NetVanguard.App.App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                            {
                                ActiveLimits.Clear();
                                foreach (var limit in limitsList)
                                {
                                    ActiveLimits.Add(limit);
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching limits: {ex}");
            }
        }

        private async Task<string> sendCommand(CommandMessage cmd)
        {
            using var pipeClient = new System.IO.Pipes.NamedPipeClientStream(
                ".", PipeConstants.CommandPipeName, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);

            await pipeClient.ConnectAsync(3000);
            
            using var writer = new System.IO.StreamWriter(pipeClient) { AutoFlush = true };
            await writer.WriteLineAsync(JsonSerializer.Serialize(cmd));

            using var reader = new System.IO.StreamReader(pipeClient);
            return await reader.ReadLineAsync() ?? string.Empty;
        }
    }
}
