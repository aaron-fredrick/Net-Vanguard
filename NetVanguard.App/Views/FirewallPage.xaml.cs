using Microsoft.UI.Xaml.Controls;
using NetVanguard.App.ViewModels;

namespace NetVanguard.App.Views
{
    public sealed partial class FirewallPage : Page
    {
        public FirewallViewModel ViewModel { get; }

        public FirewallPage()
        {
            ViewModel = new FirewallViewModel();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private async void AddNewRule_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            RuleNameInput.Text = string.Empty;
            AppPathInput.Text = string.Empty;
            ActionInput.SelectedIndex = 0; // Block
            DirectionInput.SelectedIndex = 1; // Outbound

            NewRuleDialog.XamlRoot = this.XamlRoot;
            await NewRuleDialog.ShowAsync();
        }

        private void RuleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewRuleDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(RuleNameInput.Text);
        }

        private async void NewRuleDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var rule = new NetVanguard.Core.Models.FirewallRuleModel
            {
                Name = RuleNameInput.Text,
                ApplicationName = string.IsNullOrWhiteSpace(AppPathInput.Text) ? null : AppPathInput.Text,
                Action = ActionInput.SelectedIndex == 0 ? NetVanguard.Core.Models.FirewallAction.Block : NetVanguard.Core.Models.FirewallAction.Allow,
                Direction = DirectionInput.SelectedIndex == 0 ? NetVanguard.Core.Models.FirewallDirection.Inbound : NetVanguard.Core.Models.FirewallDirection.Outbound,
                Enabled = true
            };

            await ViewModel.AddRuleCommand.ExecuteAsync(rule);
        }
    }
}
