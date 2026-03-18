using Microsoft.UI.Xaml.Controls;
using NetVanguard.App.ViewModels;

namespace NetVanguard.App.Views
{
    public sealed partial class DashboardPage : Page
    {
        public MainViewModel ViewModel { get; }

        public DashboardPage()
        {
            ViewModel = new MainViewModel();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private async void SetLimit_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is NetVanguard.Core.Models.NetworkApplication app)
            {
                var quotaInput = new TextBox { Header = "Data Quota (MB)", PlaceholderText = "e.g. 50" };
                var throttleInput = new TextBox { Header = "Bandwidth Limit (Bytes/s)", PlaceholderText = "e.g. 100000" };
                
                if (app.DataQuotaBytes.HasValue) quotaInput.Text = (app.DataQuotaBytes.Value / (1024 * 1024)).ToString();
                if (app.ThrottleLimitBps.HasValue) throttleInput.Text = app.ThrottleLimitBps.Value.ToString();

                var dialogContent = new StackPanel { Spacing = 12 };
                dialogContent.Children.Add(new TextBlock { Text = $"Target: {app.ProcessName}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                dialogContent.Children.Add(new TextBlock { Text = "Enter numeric values to apply limits. Leave blank to disable.", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray), TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });
                dialogContent.Children.Add(quotaInput);
                dialogContent.Children.Add(throttleInput);

                var dialog = new ContentDialog
                {
                    Title = "Set Traffic Limits",
                    PrimaryButtonText = "Apply",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = dialogContent,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    long? quota = string.IsNullOrWhiteSpace(quotaInput.Text) ? null : long.Parse(quotaInput.Text) * 1024 * 1024;
                    long? throttle = string.IsNullOrWhiteSpace(throttleInput.Text) ? null : long.Parse(throttleInput.Text);

                    ViewModel.SendSetLimitCommand(app.ProcessName, quota, throttle);
                }
            }
        }
    }
}
