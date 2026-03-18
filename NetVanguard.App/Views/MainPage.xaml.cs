using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace NetVanguard.App.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                ContentFrame.Navigate(typeof(DashboardPage));
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
                }
            }
            else if (args.SelectedItem is NavigationViewItem navItem)
            {
                switch (navItem.Tag?.ToString())
                {
                    case "Dashboard":
                        if (ContentFrame.CurrentSourcePageType != typeof(DashboardPage))
                        {
                            ContentFrame.Navigate(typeof(DashboardPage));
                        }
                        break;
                    case "Limits":
                        if (ContentFrame.CurrentSourcePageType != typeof(LimitsPage))
                        {
                            ContentFrame.Navigate(typeof(LimitsPage));
                        }
                        break;
                    case "Firewall":
                        if (ContentFrame.CurrentSourcePageType != typeof(FirewallPage))
                        {
                            ContentFrame.Navigate(typeof(FirewallPage));
                        }
                        break;
                }
            }
        }
    }
}
