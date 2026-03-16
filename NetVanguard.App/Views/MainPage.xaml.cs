using Microsoft.UI.Xaml.Controls;
using NetVanguard.App.ViewModels;

namespace NetVanguard.App.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            ViewModel = new MainViewModel();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }
    }
}
