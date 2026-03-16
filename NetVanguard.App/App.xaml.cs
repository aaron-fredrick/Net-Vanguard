using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Security.Principal;
using System.IO;

namespace NetVanguard.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public Window? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!IsRunAsAdmin())
            {
                Elevate();
                return;
            }

            EnsureDaemonRunning();

            MainWindow = new Window();

            if (MainWindow.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                MainWindow.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            MainWindow.Title = "Net-Vanguard Dashboard";
            
            // Set App window size (WinUI 3 requires interop for this)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow!);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1100, Height = 800 });
            
            MainWindow!.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private static bool IsRunAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void Elevate()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
            }
            catch (Exception) { }

            Application.Current.Exit();
        }

        private static void EnsureDaemonRunning()
        {
            const string daemonName = "NetVanguard.Daemon";
            if (Process.GetProcessesByName(daemonName).Length > 0) return;

            // Search paths for development and production
            string[] searchPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetVanguard.Daemon.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "NetVanguard.Daemon", "bin", "Debug", "net8.0", "NetVanguard.Daemon.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "NetVanguard.Daemon", "bin", "Release", "net8.0", "NetVanguard.Daemon.exe")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true, Verb = "runas" });
                        return;
                    }
                    catch { }
                }
            }
        }
    }
}
