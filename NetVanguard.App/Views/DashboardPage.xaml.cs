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
    }
}
