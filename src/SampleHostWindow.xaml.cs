using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using Autodesk.DataExchange;
using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.Core.Interface;
using Autodesk.DataExchange.Core.Models;
using Autodesk.DataExchange.Interface;
using Autodesk.DataExchange.UI.Core;
using Autodesk.DataExchange.UI.Core.Interfaces;
using WindowStateEnum = Autodesk.DataExchange.UI.Core.Enums.WindowState;

namespace SampleConnector
{
    public partial class SampleHostWindow : Window
    {
        private CustomReadWriteModel customReadWriteModel;
        private SDKOptionsDefaultSetup sdkOptions;
        private IClient client;
        private bool connectorInitialized;
        private bool isDestroying;

        public SampleHostWindow()
        {
            this.InitializeComponent();
            this.RegisterSystemLanguage();
            this.Loaded += this.OnWindowLoaded;
            this.Closed += (_, __) => this.Destroy();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.OnWindowLoaded;

            if (this.connectorInitialized)
            {
                return;
            }

            var windowHelper = new WindowInteropHelper(this);
            windowHelper.EnsureHandle();

            if (windowHelper.Handle == IntPtr.Zero)
            {
                MessageBox.Show(
                    this,
                    "Failed to obtain the host window handle. The Connector UI cannot connect to this application.",
                    "Sample Connector",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                this.StatusText.Text = "Initializing connector (authenticating)...";
                await this.InitializeConnectorAsync(windowHelper.Handle).ConfigureAwait(true);
                this.connectorInitialized = true;
                this.StatusText.Text = "Connector is running. Keep this window open while using the Connector UI.";
                this.Activate();
                this.Focus();
            }
            catch (Exception ex)
            {
                this.sdkOptions?.Logger?.Error(ex);
                MessageBox.Show(
                    this,
                    $"Failed to start the Data Exchange Connector:\n\n{ex.Message}",
                    "Sample Connector",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.StatusText.Text = "Connector failed to start. See the error dialog for details.";
            }
        }

        public void Destroy()
        {
            if (this.isDestroying)
            {
                return;
            }

            this.isDestroying = true;

            if (this.customReadWriteModel != null)
            {
                var exchanges = this.customReadWriteModel.GetLocalExchanges();
                if (exchanges != null)
                {
                    this.sdkOptions?.Storage.Add("LocalExchanges", exchanges);
                    this.sdkOptions?.Storage.Save("LocalExchanges");
                }

                if (this.customReadWriteModel.Bridge != null)
                {
                    this.customReadWriteModel.Bridge.SetWindowState(WindowStateEnum.Close);
                    InteropBridgeFactory.DestroyAsync(this.customReadWriteModel.Bridge);
                    this.customReadWriteModel.Bridge = null;
                }
            }
        }

        private void RegisterSystemLanguage()
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        private async Task InitializeConnectorAsync(IntPtr hostWindowHandle)
        {
            var authClientId = ConfigurationManager.AppSettings["AuthClientId"];
            var authClientSecret = ConfigurationManager.AppSettings["AuthClientSecret"];
            var authCallback = ConfigurationManager.AppSettings["AuthCallback"];
            var logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var connectorName = ConfigurationManager.AppSettings["ConnectorName"];
            var connectorVersion = ConfigurationManager.AppSettings["ConnectorVersion"];
            var hostApplicationName = ConfigurationManager.AppSettings["HostApplicationName"];
            var hostApplicationVersion = ConfigurationManager.AppSettings["HostApplicationVersion"];

            if (string.IsNullOrEmpty(authClientId))
            {
                throw new ConfigurationErrorsException("AuthClientId is missing from App.config. Please ensure the config file is properly configured.");
            }

            if (string.IsNullOrEmpty(authCallback))
            {
                throw new ConfigurationErrorsException("AuthCallback is missing from App.config. Please ensure the config file is properly configured.");
            }

            if (!authCallback.EndsWith("/"))
            {
                throw new ConfigurationErrorsException("AuthCallback URL must end with a trailing slash '/'. Example: http://127.0.0.1:63212/");
            }

            if (string.IsNullOrEmpty(connectorName) || string.IsNullOrEmpty(connectorVersion) ||
                string.IsNullOrEmpty(hostApplicationName) || string.IsNullOrEmpty(hostApplicationVersion))
            {
                throw new ConfigurationErrorsException("ConnectorName, ConnectorVersion, HostApplicationName, and HostApplicationVersion are required in App.config.");
            }

            this.sdkOptions = new SDKOptionsDefaultSetup()
            {
                CallBack = authCallback,
                ClientId = authClientId,
                ClientSecret = authClientSecret,
                ConnectorName = connectorName,
                ConnectorVersion = connectorVersion,
                HostApplicationName = hostApplicationName,
                HostApplicationVersion = hostApplicationVersion,
            };

            // Create the client off the UI thread so OAuth does not block message handling.
            await Task.Run(() =>
            {
                this.client = new Client(this.sdkOptions);
            }).ConfigureAwait(true);

            if (this.GetLogLevel(logLevel) == LogLevel.Debug)
            {
                this.SetDebugLogLevel(this.sdkOptions?.Logger);
            }

            // Finish authentication before the Connector UI connects and requests a token.
            await this.sdkOptions.AuthProvider.GetAuthTokenAsync().ConfigureAwait(true);

            this.customReadWriteModel = new CustomReadWriteModel(this.client);
            this.LoadLocalExchanges();

            var bridgeOptions = InteropBridgeOptions.FromClient(this.client);
            bridgeOptions.Exchange = this.customReadWriteModel;
            bridgeOptions.Invoker = new MainThreadInvoker(this.Dispatcher);
            bridgeOptions.HostWindowHandle = hostWindowHandle;

            var bridge = InteropBridgeFactory.Create(bridgeOptions);
            this.customReadWriteModel.Bridge = bridge;

            bridge.ClientStateChanged += (sender, e) =>
            {
                if (e.IsConnected)
                {
                    this.customReadWriteModel.Bridge.SetDocumentName("Sample Document");
                }
            };

            await this.InitializeAndLaunchConnectorUiAsync(bridge).ConfigureAwait(true);
        }

        private async Task InitializeAndLaunchConnectorUiAsync(IInteropBridge interopBridge)
        {
            await interopBridge.InitializeAsync().ConfigureAwait(true);
            await interopBridge.LaunchConnectorUiAsync().ConfigureAwait(true);
        }

        private LogLevel GetLogLevel(string logLevel)
        {
            LogLevel parsedlogLevel;
            bool canConvertToEnum = Enum.TryParse<LogLevel>(logLevel, true, out parsedlogLevel);
            return canConvertToEnum ? parsedlogLevel : LogLevel.Error;
        }

        private void SetDebugLogLevel(ILogger logger)
        {
            logger?.SetDebugLogLevel();
        }

        private void LoadLocalExchanges()
        {
            var exchanges = this.sdkOptions.Storage.Get<List<DataExchange>>("LocalExchanges");
            if (exchanges != null)
            {
                this.customReadWriteModel.SetLocalExchanges(exchanges);
            }
        }
    }
}
