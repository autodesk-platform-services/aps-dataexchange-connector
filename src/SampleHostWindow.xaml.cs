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

        public SampleHostWindow()
        {
            this.InitializeComponent();
            this.RegisterSystemLanguage();
            this.InitializeConnector();
        }

        public void Destroy()
        {
            if (this.customReadWriteModel != null)
            {
                var exchanges = this.customReadWriteModel.GetLocalExchanges();
                if (exchanges != null)
                {
                    this.sdkOptions?.Storage.Add("LocalExchanges", exchanges);
                }

                this.sdkOptions?.Storage.Save();

                // Destroy interop bridge object and Connector UI
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

        private void InitializeConnector()
        {
            // Read configuration
            var authClientId = ConfigurationManager.AppSettings["AuthClientId"];
            var authClientSecret = ConfigurationManager.AppSettings["AuthClientSecret"];
            var authCallback = ConfigurationManager.AppSettings["AuthCallback"];
            var logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var connectorName = ConfigurationManager.AppSettings["ConnectorName"];
            var connectorVersion = ConfigurationManager.AppSettings["ConnectorVersion"];
            var hostApplicationName = ConfigurationManager.AppSettings["HostApplicationName"];
            var hostApplicationVersion = ConfigurationManager.AppSettings["HostApplicationVersion"];

            // Validate required configuration
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

            // Step 1: Create SDK options (using PKCE auth flow - no client secret needed)
            this.sdkOptions = new SDKOptionsDefaultSetup()
            {
                CallBack = authCallback,
                ClientId = authClientId,
                ConnectorName = connectorName,
                ConnectorVersion = connectorVersion,
                HostApplicationName = hostApplicationName,
                HostApplicationVersion = hostApplicationVersion,
            };

            // Step 2: Create the Client (this triggers authentication)
            this.client = new Client(this.sdkOptions);

            // Configure logging
            if (this.GetLogLevel(logLevel) == LogLevel.Debug)
            {
                this.SetDebugLogLevel(this.sdkOptions?.Logger);
            }

            // Step 3: Create the exchange model with the client
            this.customReadWriteModel = new CustomReadWriteModel(this.client);

            // Load locally cached exchanges
            this.LoadLocalExchanges();

            // Step 4: Create InteropBridgeOptions from the client
            var bridgeOptions = InteropBridgeOptions.FromClient(this.client);
            bridgeOptions.Exchange = this.customReadWriteModel;
            bridgeOptions.Invoker = new MainThreadInvoker(this.Dispatcher);
            bridgeOptions.FeedbackUrl = "https://some.feedback.url";
            bridgeOptions.HostWindowHandle = new WindowInteropHelper(this).Handle;

            // Step 5: Create the bridge and assign it to the model
            var bridge = InteropBridgeFactory.Create(bridgeOptions);
            this.customReadWriteModel.Bridge = bridge;

            // Subscribe to ClientStateChanged event for UI state notifications
            bridge.ClientStateChanged += (sender, e) =>
            {
                if (e.IsConnected)
                {
                    // Set the document name only after the Connector UI is connected.
                    // If SetDocumentName is called too early, it will have no effect
                    // because the Connector UI does not yet exist at that point.
                    this.customReadWriteModel.Bridge.SetDocumentName("Sample Document");
                }
            };

            // Initialize and launch the connector UI asynchronously
            _ = this.InitializeAndLaunchConnectorUi(bridge);
        }

        private async Task InitializeAndLaunchConnectorUi(IInteropBridge interopBridge)
        {
            try
            {
                // Initialize the interop bridge first
                await interopBridge.InitializeAsync();

                // Then launch the connector UI
                await interopBridge.LaunchConnectorUiAsync();
            }
            catch (Exception ex)
            {
                // Log errors during initialization/launching
                this.sdkOptions?.Logger?.Error(ex);
                throw;
            }
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

        private void EnableHttpLogsForDebugging()
        {
            (this.client as Client)?.EnableHttpDebugLogging();
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
