using YKEnroll.Win.ViewModels;
using YKEnroll.Win.Views.Windows;
using System;
using System.Windows;
using YKEnroll.Win.Styles;

namespace YKEnroll.Win
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    internal sealed partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Render style colors.
            var accentBrush = TryFindResource("AccentColorBrush") as System.Windows.Media.SolidColorBrush;
            if (accentBrush != null) accentBrush.Color.CreateAccentColors();

            // Set up the main window
            var mainWindowViewModel = new MainViewModel();
            var mainWindow = new MainView { DataContext = mainWindowViewModel };
            mainWindow.Show();
        }

        private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;            
            MessageBox.Show("An unhandled error has occured. The application will terminate! Exception: " + ex.Message);
            Application.Current.Shutdown();
        }
    }    

}
