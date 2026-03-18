using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using NetVanguard.App.Services;
using NetVanguard.Core.Models;
using SkiaSharp;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace NetVanguard.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ITrafficClientService _trafficClient;
        private readonly ObservableCollection<ObservableValue> _downloadValues = new();
        private readonly ObservableCollection<ObservableValue> _uploadValues = new();
        
        private TrafficUpdateMessage? _lastMessage;

        private ShowcaseType _currentShowcase = ShowcaseType.Process;
        public ShowcaseType CurrentShowcase
        {
            get => _currentShowcase;
            set
            {
                if (SetProperty(ref _currentShowcase, value))
                {
                    OnPropertyChanged(nameof(IsProcessView));
                    OnPropertyChanged(nameof(IsDetailedView));
                    OnPropertyChanged(nameof(IsAppListView));
                    OnPropertyChanged(nameof(IsAdapterView));
                    OnPropertyChanged(nameof(IsDomainView));
                    refreshCurrentView();
                }
            }
        }

        public bool IsProcessView => CurrentShowcase == ShowcaseType.Process;
        public bool IsDetailedView => CurrentShowcase == ShowcaseType.Detailed;
        public bool IsAppListView => IsProcessView || IsDetailedView;
        public bool IsAdapterView => CurrentShowcase == ShowcaseType.Adapter;
        public bool IsDomainView => CurrentShowcase == ShowcaseType.Domain;

        private string _sortColumn = "Download";
        public string SortColumn
        {
            get => _sortColumn;
            set
            {
                if (SetProperty(ref _sortColumn, value))
                {
                    OnPropertyChanged(nameof(SortArrowName));
                    OnPropertyChanged(nameof(SortArrowDownload));
                    OnPropertyChanged(nameof(SortArrowUpload));
                    OnPropertyChanged(nameof(SortArrowActivity));
                    OnPropertyChanged(nameof(SortArrowDomain));
                    OnPropertyChanged(nameof(SortArrowIP));
                    UpdateSortBrushes();
                    refreshCurrentView();
                }
            }
        }

        private bool _isSortDescending = true;
        public bool IsSortDescending
        {
            get => _isSortDescending;
            set
            {
                if (SetProperty(ref _isSortDescending, value))
                {
                    OnPropertyChanged(nameof(SortArrowName));
                    OnPropertyChanged(nameof(SortArrowDownload));
                    OnPropertyChanged(nameof(SortArrowUpload));
                    OnPropertyChanged(nameof(SortArrowActivity));
                    OnPropertyChanged(nameof(SortArrowDomain));
                    OnPropertyChanged(nameof(SortArrowIP));
                    refreshCurrentView();
                }
            }
        }

        [ObservableProperty]
        public partial ObservableCollection<NetworkApplication> ActiveApplications { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<AdapterTraffic> AdapterTraffic { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<DomainTraffic> DomainTraffic { get; set; } = new();

        [ObservableProperty]
        public partial bool IsAppStatsLoading { get; set; }

        [ObservableProperty]
        public partial bool IsDomainStatsLoading { get; set; }

        [ObservableProperty]
        public partial NetworkApplication? SelectedApplication { get; set; }

        [ObservableProperty]
        public partial DomainTraffic? SelectedDomain { get; set; }

        partial void OnSelectedApplicationChanged(NetworkApplication? value)
        {
            OnPropertyChanged(nameof(IsAppDetailVisible));
            OnPropertyChanged(nameof(IsDetailPaneVisible));
            OnPropertyChanged(nameof(AppDetailName));
            OnPropertyChanged(nameof(AppDetailPath));
            OnPropertyChanged(nameof(AppDetailDomains));

            if (value != null)
            {
                SelectedDomain = null; // Mutually exclusive
                // Trigger loading state if stats are currently empty/zero
                if (value.LifetimeTotalBytesSent == 0 && value.LifetimeTotalBytesReceived == 0)
                    IsAppStatsLoading = true;
            }
            else IsAppStatsLoading = false;
        }

        partial void OnSelectedDomainChanged(DomainTraffic? value)
        {
            OnPropertyChanged(nameof(IsDomainDetailVisible));
            OnPropertyChanged(nameof(IsDetailPaneVisible));
            OnPropertyChanged(nameof(DomainDetailName));
            OnPropertyChanged(nameof(DomainDetailIp));
            OnPropertyChanged(nameof(DomainDetailApps));

            if (value != null)
            {
                SelectedApplication = null; // Mutually exclusive
                if (value.LifetimeTotalBytesSent == 0 && value.LifetimeTotalBytesReceived == 0)
                    IsDomainStatsLoading = true;
            }
            else IsDomainStatsLoading = false;
        }

        public bool IsAppDetailVisible => SelectedApplication != null;
        public bool IsDomainDetailVisible => SelectedDomain != null;
        public bool IsDetailPaneVisible => IsAppDetailVisible || IsDomainDetailVisible;

        // Safe Bind Proxies for Detail Pane
        public string AppDetailName => SelectedApplication?.ProcessName ?? string.Empty;
        public string AppDetailPath => SelectedApplication?.ExecutablePath ?? string.Empty;
        public List<string> AppDetailDomains => SelectedApplication?.ConnectedDomains ?? new();

        public string DomainDetailName => SelectedDomain?.DomainName ?? string.Empty;
        public string DomainDetailIp => SelectedDomain?.RemoteIp ?? string.Empty;
        public List<string> DomainDetailApps => SelectedDomain?.EngagingProcesses ?? new();
        public List<string> DomainDetailDns => SelectedDomain?.DnsRecords ?? new();

        public string AppDetailQuota => SelectedApplication?.DataQuotaBytes.HasValue == true 
            ? NetworkApplication.FormatTraffic(SelectedApplication.DataQuotaBytes.Value) 
            : "Unlimited";
            
        public string AppDetailThrottle => SelectedApplication?.ThrottleLimitBps.HasValue == true 
            ? $"{SelectedApplication.ThrottleLimitBps.Value / 1024.0:F1} KB/s" 
            : "Full Speed";

        public string AppLifeTotal => SelectedApplication == null ? "0 B" 
            : $"↓ {NetworkApplication.FormatTraffic(SelectedApplication.LifetimeTotalBytesReceived)}  ↑ {NetworkApplication.FormatTraffic(SelectedApplication.LifetimeTotalBytesSent)}";
        
        public string AppLifePeak => SelectedApplication == null ? "0 B" 
            : $"↓ {NetworkApplication.FormatTraffic(SelectedApplication.LifetimeMaxBytesReceived)}/s  ↑ {NetworkApplication.FormatTraffic(SelectedApplication.LifetimeMaxBytesSent)}/s";

        public string AppLifeBlocked => SelectedApplication == null ? "0 B"
            : NetworkApplication.FormatTraffic(SelectedApplication.LifetimeTotalBytesBlocked);

        public string DomainLifeTotal => SelectedDomain == null ? "0 B" 
            : $"↓ {NetworkApplication.FormatTraffic(SelectedDomain.LifetimeTotalBytesReceived)}  ↑ {NetworkApplication.FormatTraffic(SelectedDomain.LifetimeTotalBytesSent)}";
        
        public string DomainLifePeak => SelectedDomain == null ? "0 B" 
            : $"↓ {NetworkApplication.FormatTraffic(SelectedDomain.LifetimeMaxBytesReceived)}/s  ↑ {NetworkApplication.FormatTraffic(SelectedDomain.LifetimeMaxBytesSent)}/s";

        public string DomainLifeBlocked => SelectedDomain == null ? "0 B"
            : NetworkApplication.FormatTraffic(SelectedDomain.LifetimeTotalBytesBlocked);

        private ISeries[] _trafficSeries = Array.Empty<ISeries>();
        public ISeries[] TrafficSeries
        {
            get => _trafficSeries;
            set => SetProperty(ref _trafficSeries, value);
        }

        public string SortArrowName => getArrow("Name");
        public string SortArrowDownload => getArrow("Download");
        public string SortArrowUpload => getArrow("Upload");
        public string SortArrowActivity => getArrow("Activity");
        public string SortArrowDomain => getArrow("Domain");
        public string SortArrowIP => getArrow("IP");

        private Brush _sortBrushName = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushName => _sortBrushName;

        private Brush _sortBrushDownload = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushDownload => _sortBrushDownload;

        private Brush _sortBrushUpload = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushUpload => _sortBrushUpload;

        private Brush _sortBrushActivity = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushActivity => _sortBrushActivity;

        private Brush _sortBrushDomain = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushDomain => _sortBrushDomain;

        private Brush _sortBrushIP = new SolidColorBrush(Colors.Transparent);
        public Brush SortBrushIP => _sortBrushIP;

        private string getArrow(string column)
        {
            if (SortColumn != column) return string.Empty;
            return IsSortDescending ? " ▼" : " ▲";
        }

        private void UpdateSortBrushes()
        {
            try
            {
                if (Application.Current == null) return;

                var highlight = (Brush)Application.Current.Resources["SystemControlBackgroundAltMediumHighBrush"];
                var transparent = new SolidColorBrush(Colors.Transparent);

                _sortBrushName = SortColumn == "Name" ? highlight : transparent;
                _sortBrushDownload = SortColumn == "Download" ? highlight : transparent;
                _sortBrushUpload = SortColumn == "Upload" ? highlight : transparent;
                _sortBrushActivity = SortColumn == "Activity" ? highlight : transparent;
                _sortBrushDomain = SortColumn == "Domain" ? highlight : transparent;
                _sortBrushIP = SortColumn == "IP" ? highlight : transparent;

                OnPropertyChanged(nameof(SortBrushName));
                OnPropertyChanged(nameof(SortBrushDownload));
                OnPropertyChanged(nameof(SortBrushUpload));
                OnPropertyChanged(nameof(SortBrushActivity));
                OnPropertyChanged(nameof(SortBrushDomain));
                OnPropertyChanged(nameof(SortBrushIP));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating sort brushes: {ex.Message}");
            }
        }

        public MainViewModel()
        {
            _trafficClient = new TrafficClientService();

            try
            {
                for (int i = 0; i < 30; i++)
                {
                    _downloadValues.Add(new ObservableValue(0));
                    _uploadValues.Add(new ObservableValue(0));
                }

                TrafficSeries = new ISeries[]
                {
                    new LineSeries<ObservableValue> { Values = _downloadValues, Name = "Download (MB)", Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(15)), Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 }, GeometrySize = 0, LineSmoothness = 1 },
                    new LineSeries<ObservableValue> { Values = _uploadValues, Name = "Upload (MB)", Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(15)), Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 }, GeometrySize = 0, LineSmoothness = 1 }
                };

                _trafficClient.OnMessageReceived += (s, msg) => UpdateData(msg);
                _trafficClient.StartListening();
                
                UpdateSortBrushes();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MainViewModel constructor components: {ex.Message}");
            }
        }

        public Axis[] XAxes { get; } = 
        { 
            new Axis 
            { 
                IsVisible = false,
                CrosshairPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(100)) 
                { 
                    StrokeThickness = 2, 
                    PathEffect = new DashEffect(new float[] { 6, 6 }) 
                } 
            } 
        };
        public Axis[] YAxes { get; } = 
        { 
            new Axis 
            { 
                Name = "MB", 
                NamePaint = new SolidColorPaint(SKColors.Gray), 
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50)) 
                { 
                    StrokeThickness = 1, 
                    PathEffect = new DashEffect(new float[] { 3, 3 }) 
                }
            } 
        };

        [RelayCommand]
        private void CloseDetail()
        {
            SelectedApplication = null;
            SelectedDomain = null;
        }

        [RelayCommand]
        private void ToggleSort(string column)
        {
            if (SortColumn == column) IsSortDescending = !IsSortDescending;
            else { SortColumn = column; IsSortDescending = true; }
        }

        [RelayCommand]
        private void SwitchView(string viewType)
        {
            if (Enum.TryParse<ShowcaseType>(viewType, out var type))
            {
                CurrentShowcase = type;
                CloseDetail(); // Clear selection when switching categories
            }
        }

        public void UpdateData(TrafficUpdateMessage message)
        {
            _lastMessage = message;
            App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (message.Applications == null) return;

                    double totalDl = message.Applications.Sum(a => a.BytesReceived) / (1024.0 * 1024.0);
                    double totalUl = message.Applications.Sum(a => a.BytesSent) / (1024.0 * 1024.0);
                    
                    _downloadValues.Add(new ObservableValue(totalDl));
                    _uploadValues.Add(new ObservableValue(totalUl));
                    
                    if (_downloadValues.Count > 30) _downloadValues.RemoveAt(0);
                    if (_uploadValues.Count > 30) _uploadValues.RemoveAt(0);

                    refreshCurrentView();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in UpdateData Dispatcher: {ex.Message}");
                }
            });
        }

        public async void SendSetLimitCommand(string processName, long? quotaBytes, long? throttleBps)
        {
            try
            {
                var payload = new 
                { 
                    ProcessName = processName, 
                    DataQuotaBytes = quotaBytes, 
                    ThrottleLimitBps = throttleBps 
                };

                var cmd = new CommandMessage
                {
                    Command = CommandType.SetLimit,
                    Payload = System.Text.Json.JsonSerializer.Serialize(payload)
                };

                var pipeClient = new System.IO.Pipes.NamedPipeClientStream(
                    ".", NetVanguard.Core.Infrastructure.PipeConstants.CommandPipeName, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);

                await pipeClient.ConnectAsync(5000);

                using var writer = new System.IO.StreamWriter(pipeClient) { AutoFlush = true };
                await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(cmd));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending SetLimit command to Daemon: {ex.Message}");
            }
        }

        private void refreshCurrentView()
        {
            if (_lastMessage == null) return;

            try
            {
                switch (CurrentShowcase)
                {
                    case ShowcaseType.Process:
                        syncApps(_lastMessage.Applications ?? new());
                        break;
                    case ShowcaseType.Detailed:
                        syncApps((_lastMessage.Applications ?? new()).Where(a => !isSystemProcess(a)));
                        break;
                    case ShowcaseType.Adapter:
                        syncAdapters(_lastMessage.Adapters ?? new());
                        break;
                    case ShowcaseType.Domain:
                        syncDomains(_lastMessage.Domains ?? new());
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in refreshCurrentView: {ex.Message}");
            }
        }

        private bool isSystemProcess(NetworkApplication app)
        {
            if (app.IsWindowsService) return true;
            string name = app.ProcessName?.ToLower() ?? string.Empty;
            return name == "svchost" || name == "rundll32" || name == "lsass" || name == "services" || name == "system" || name == "idle";
        }

        private void syncApps(IEnumerable<NetworkApplication> apps)
        {
            var sorted = SortColumn switch
            {
                "Name" => IsSortDescending ? apps.OrderByDescending(a => a.ProcessName) : apps.OrderBy(a => a.ProcessName),
                "Download" => IsSortDescending ? apps.OrderByDescending(a => a.BytesReceived) : apps.OrderBy(a => a.BytesReceived),
                "Upload" => IsSortDescending ? apps.OrderByDescending(a => a.BytesSent) : apps.OrderBy(a => a.BytesSent),
                "Activity" => IsSortDescending ? apps.OrderByDescending(a => a.LastSeen) : apps.OrderBy(a => a.LastSeen),
                _ => apps.OrderByDescending(a => a.BytesReceived)
            };
            syncCollection(ActiveApplications, sorted.Take(30).ToList(), (a, b) => a.ProcessId == b.ProcessId, (target, source) => {
                target.BytesReceived = source.BytesReceived;
                target.BytesSent = source.BytesSent;
                target.LastSeen = source.LastSeen;
                target.ProcessName = source.ProcessName;
                target.ConnectedDomains = source.ConnectedDomains;
                
                // Copy Lifetime Stats
                target.LifetimeTotalBytesSent = source.LifetimeTotalBytesSent;
                target.LifetimeTotalBytesReceived = source.LifetimeTotalBytesReceived;
                target.LifetimeMaxBytesSent = source.LifetimeMaxBytesSent;
                target.LifetimeMaxBytesReceived = source.LifetimeMaxBytesReceived;
                target.LifetimeTotalBytesBlocked = source.LifetimeTotalBytesBlocked;

                if (SelectedApplication?.ProcessId == target.ProcessId)
                {
                    OnPropertyChanged(nameof(SelectedApplication));
                    OnPropertyChanged(nameof(AppLifeTotal));
                    OnPropertyChanged(nameof(AppLifePeak));
                    OnPropertyChanged(nameof(AppLifeBlocked));
                }
                
                // Clear loading state after sync
                IsAppStatsLoading = false;
            });
        }

        private void syncAdapters(IEnumerable<AdapterTraffic> adapters)
        {
            var sorted = SortColumn switch
            {
                "Name" => IsSortDescending ? adapters.OrderByDescending(a => a.Name) : adapters.OrderBy(a => a.Name),
                "Download" => IsSortDescending ? adapters.OrderByDescending(a => a.BytesReceived) : adapters.OrderBy(a => a.BytesReceived),
                "Upload" => IsSortDescending ? adapters.OrderByDescending(a => a.BytesSent) : adapters.OrderBy(a => a.BytesSent),
                _ => IsSortDescending ? adapters.OrderByDescending(a => a.BytesReceived) : adapters.OrderBy(a => a.BytesReceived)
            };
            syncCollection(AdapterTraffic, sorted.ToList(), (a, b) => a.Name == b.Name, (target, source) => {
                target.BytesReceived = source.BytesReceived;
                target.BytesSent = source.BytesSent;
            });
        }

        private void syncDomains(IEnumerable<DomainTraffic> domains)
        {
            var sorted = SortColumn switch
            {
                "Domain" => IsSortDescending ? domains.OrderByDescending(a => a.DomainName) : domains.OrderBy(a => a.DomainName),
                "IP" => IsSortDescending ? domains.OrderByDescending(a => a.RemoteIp) : domains.OrderBy(a => a.RemoteIp),
                "Download" => IsSortDescending ? domains.OrderByDescending(a => a.BytesReceived) : domains.OrderBy(a => a.BytesReceived),
                "Upload" => IsSortDescending ? domains.OrderByDescending(a => a.BytesSent) : domains.OrderBy(a => a.BytesSent),
                _ => IsSortDescending ? domains.OrderByDescending(a => a.BytesReceived) : domains.OrderBy(a => a.BytesReceived)
            };
            syncCollection(DomainTraffic, sorted.Take(50).ToList(), (a, b) => a.RemoteIp == b.RemoteIp, (target, source) => {
                target.BytesReceived = source.BytesReceived;
                target.BytesSent = source.BytesSent;
                target.DomainName = source.DomainName;
                target.EngagingProcesses = source.EngagingProcesses;
                
                // Copy Lifetime Stats
                target.LifetimeTotalBytesSent = source.LifetimeTotalBytesSent;
                target.LifetimeTotalBytesReceived = source.LifetimeTotalBytesReceived;
                target.LifetimeMaxBytesSent = source.LifetimeMaxBytesSent;
                target.LifetimeMaxBytesReceived = source.LifetimeMaxBytesReceived;
                target.LifetimeTotalBytesBlocked = source.LifetimeTotalBytesBlocked;

                if (SelectedDomain?.RemoteIp == target.RemoteIp)
                {
                    OnPropertyChanged(nameof(SelectedDomain));
                    OnPropertyChanged(nameof(DomainLifeTotal));
                    OnPropertyChanged(nameof(DomainLifePeak));
                    OnPropertyChanged(nameof(DomainLifeBlocked));
                }

                IsDomainStatsLoading = false;
            });
        }

        private void syncCollection<T>(ObservableCollection<T> current, List<T> newList, Func<T, T, bool> identityMatch, Action<T, T> updateAction) where T : class
        {
            try
            {
                // 1. Remove items that no longer exist
                for (int i = current.Count - 1; i >= 0; i--)
                {
                    if (!newList.Any(n => identityMatch(n, current[i])))
                    {
                        var dropped = current[i];
                        current.RemoveAt(i);
                        if (ReferenceEquals(SelectedApplication, dropped)) SelectedApplication = null;
                        if (ReferenceEquals(SelectedDomain, dropped)) SelectedDomain = null;
                    }
                }

                // 2. Add or update items
                for (int i = 0; i < newList.Count; i++)
                {
                    var newItem = newList[i];
                    var existing = current.FirstOrDefault(c => identityMatch(c, newItem));

                    if (existing == null)
                    {
                        // Add if it's within bounds or just add to end (ListView will handle order if bound to sorted collection)
                        // Actually, to maintain visual order without flicker, we should insert at correct index
                        if (i < current.Count)
                            current.Insert(i, newItem);
                        else
                            current.Add(newItem);
                    }
                    else
                    {
                        // Update content
                        updateAction(existing, newItem);
                        
                        // Fix position if necessary to match sorted list
                        int currentIndex = current.IndexOf(existing);
                        if (currentIndex != i)
                        {
                            current.Move(currentIndex, i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in syncCollection: {ex.Message}");
                // Fallback to clear if granular sync fails
                current.Clear();
                foreach (var item in newList) current.Add(item);
            }
        }
    }
}
