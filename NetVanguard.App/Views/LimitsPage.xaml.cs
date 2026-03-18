using Microsoft.UI.Xaml.Controls;
using NetVanguard.App.ViewModels;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Views
{
    public sealed partial class LimitsPage : Page
    {
        public LimitsViewModel ViewModel { get; }

        public LimitsPage()
        {
            ViewModel = new LimitsViewModel();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private async void AddLimit_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var cmbType = new ComboBox { Header = "Target Type", ItemsSource = System.Enum.GetValues(typeof(LimitTargetType)), SelectedIndex = 0, HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch };
            var txtName = new TextBox { Header = "Target Identifier", PlaceholderText = "e.g. chrome.exe or youtube.com" };
            var txtQuota = new TextBox { Header = "Data Quota (MB) [Optional]", PlaceholderText = "e.g. 50" };
            var txtThrottle = new TextBox { Header = "Bandwidth Throttle (Bytes/s) [Optional]", PlaceholderText = "e.g. 1048576" };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(cmbType);
            panel.Children.Add(txtName);
            panel.Children.Add(txtQuota);
            panel.Children.Add(txtThrottle);

            var dialog = new ContentDialog
            {
                Title = "Deploy New Constraint",
                PrimaryButtonText = "Enforce",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = panel,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(txtName.Text))
            {
                var targetType = (LimitTargetType)cmbType.SelectedItem;
                long? quota = string.IsNullOrWhiteSpace(txtQuota.Text) ? null : long.Parse(txtQuota.Text) * 1024 * 1024;
                long? throttle = string.IsNullOrWhiteSpace(txtThrottle.Text) ? null : long.Parse(txtThrottle.Text);

                ViewModel.TransmitSetLimitCommand(targetType, txtName.Text.Trim(), quota, throttle);
            }
        }

        private void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.FetchLimitsCommand();
        }

        private void DeleteLimit_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ViewModel.SelectedLimit != null)
            {
                ViewModel.TransmitDeleteLimitCommand(ViewModel.SelectedLimit.TargetType, ViewModel.SelectedLimit.TargetName);
            }
        }
    }
}
