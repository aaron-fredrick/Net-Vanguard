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
    }
}
