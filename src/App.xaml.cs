using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.DataExchange;

namespace SampleConnector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static string _installationPath;
        private SampleHostWindow hostWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create and show the main host window
            hostWindow = new SampleHostWindow();
            hostWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            hostWindow?.Destroy();
        }
    }
}
