using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NetVanguard.App.Services;
using NetVanguard.Core.Models;
using SkiaSharp;

namespace NetVanguard.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ITrafficClientService _trafficClient;

        [ObservableProperty]
        public partial ObservableCollection<NetworkApplication> ActiveApplications { get; set; } = new();

        public MainViewModel()
        {
            _trafficClient = new TrafficClientService();
            _trafficClient.OnMessageReceived += (s, msg) => UpdateData(msg.Applications);
            _trafficClient.StartListening();
        }

        public ISeries[] TrafficSeries { get; set; } =
        {
            new LineSeries<double>
            {
                Values = new ObservableCollection<double> { 0, 0, 0, 0, 0 },
                Name = "Download (MB)",
                Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometrySize = 0
            },
            new LineSeries<double>
            {
                Values = new ObservableCollection<double> { 0, 0, 0, 0, 0 },
                Name = "Upload (MB)",
                Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                GeometrySize = 0
            }
        };

        public Axis[] XAxes { get; set; } =
        {
            new Axis
            {
                Name = "Time",
                LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                NamePaint = new SolidColorPaint(SKColors.LightGray)
            }
        };

        public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                Name = "Data (MB)",
                LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                NamePaint = new SolidColorPaint(SKColors.LightGray)
            }
        };

        public void UpdateData(IEnumerable<NetworkApplication> apps)
        {
            // Ensure UI thread updates
            App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                ActiveApplications.Clear();
                double totalDownload = 0;
                double totalUpload = 0;

                foreach (var app in apps.OrderByDescending(x => x.BytesReceived + x.BytesSent).Take(20))
                {
                    ActiveApplications.Add(app);
                    totalDownload += app.BytesReceived;
                    totalUpload += app.BytesSent;
                }

                // Update Charts
                var dlValues = (ObservableCollection<double>)TrafficSeries[0].Values!;
                var ulValues = (ObservableCollection<double>)TrafficSeries[1].Values!;

                dlValues.Add(totalDownload / 1024.0 / 1024.0);
                ulValues.Add(totalUpload / 1024.0 / 1024.0);

                if (dlValues.Count > 30) dlValues.RemoveAt(0);
                if (ulValues.Count > 30) ulValues.RemoveAt(0);
            });
        }
    }
}
