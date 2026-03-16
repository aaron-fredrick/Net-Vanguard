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
using NetVanguard.App.Services;
using NetVanguard.Core.Models;
using SkiaSharp;

namespace NetVanguard.App.ViewModels
{
    public enum AppSortMode
    {
        Download,
        Upload,
        Name,
        LastActive
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly ITrafficClientService _trafficClient;
        private readonly ObservableCollection<ObservableValue> _downloadValues = new();
        private readonly ObservableCollection<ObservableValue> _uploadValues = new();

        [ObservableProperty]
#pragma warning disable MVVMTK0045
        private AppSortMode _currentSortMode = AppSortMode.Download;
#pragma warning restore MVVMTK0045

        [ObservableProperty]
#pragma warning disable MVVMTK0045
        private ObservableCollection<NetworkApplication> _activeApplications = new();
#pragma warning restore MVVMTK0045

        public MainViewModel()
        {
            // Initialize graph collections
            for (int i = 0; i < 30; i++)
            {
                _downloadValues.Add(new ObservableValue(0));
                _uploadValues.Add(new ObservableValue(0));
            }

            TrafficSeries = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = _downloadValues,
                    Name = "Download (MB)",
                    Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(20)),
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                },
                new LineSeries<ObservableValue>
                {
                    Values = _uploadValues,
                    Name = "Upload (MB)",
                    Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(20)),
                    Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                }
            };

            _trafficClient = new TrafficClientService();
            _trafficClient.OnMessageReceived += (s, msg) => UpdateData(msg.Applications);
            _trafficClient.StartListening();
        }

        public ISeries[] TrafficSeries { get; set; }
        public Axis[] XAxes { get; set; } = { new Axis { IsVisible = false } };
        public Axis[] YAxes { get; set; } = { new Axis { Name = "MB", NamePaint = new SolidColorPaint(SKColors.Gray), LabelsPaint = new SolidColorPaint(SKColors.Gray) } };

        [RelayCommand]
        public void SetSortMode(string mode)
        {
            if (Enum.TryParse<AppSortMode>(mode, out var result))
            {
                CurrentSortMode = result;
            }
        }

        public void UpdateData(IEnumerable<NetworkApplication> apps)
        {
            App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                // 1. Update Graph
                double totalDl = apps.Sum(a => a.BytesReceived) / (1024.0 * 1024.0);
                double totalUl = apps.Sum(a => a.BytesSent) / (1024.0 * 1024.0);

                _downloadValues.Add(new ObservableValue(totalDl));
                _uploadValues.Add(new ObservableValue(totalUl));
                if (_downloadValues.Count > 30) _downloadValues.RemoveAt(0);
                if (_uploadValues.Count > 30) _uploadValues.RemoveAt(0);

                // 2. Smart Update List
                var sorted = CurrentSortMode switch
                {
                    AppSortMode.Download => apps.OrderByDescending(a => a.BytesReceived),
                    AppSortMode.Upload => apps.OrderByDescending(a => a.BytesSent),
                    AppSortMode.Name => apps.OrderBy(a => a.ProcessName),
                    AppSortMode.LastActive => apps.OrderByDescending(a => a.LastSeen),
                    _ => apps.OrderByDescending(a => a.BytesReceived)
                };

                var topList = sorted.Take(20).ToList();

                // If count changed or major order changed, we might need to reset.
                // But generally, we update in place if ids match.
                bool needsReset = ActiveApplications.Count != topList.Count;
                if (!needsReset)
                {
                    for (int i = 0; i < topList.Count; i++)
                    {
                        if (ActiveApplications[i].ProcessId != topList[i].ProcessId)
                        {
                            needsReset = true;
                            break;
                        }
                    }
                }

                if (needsReset)
                {
                    ActiveApplications = new ObservableCollection<NetworkApplication>(topList);
                }
                else
                {
                    // Update properties in place (NO FLASHING)
                    for (int i = 0; i < topList.Count; i++)
                    {
                        ActiveApplications[i].BytesReceived = topList[i].BytesReceived;
                        ActiveApplications[i].BytesSent = topList[i].BytesSent;
                        ActiveApplications[i].LastSeen = topList[i].LastSeen;
                    }
                }
            });
        }
    }
}
