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
using Autodesk.DataExchange.UI.Core;
using Autodesk.DataExchange.UI.Core.Interfaces;
using WindowStateEnum = Autodesk.DataExchange.UI.Core.Enums.WindowState;

namespace SampleConnector
{

    public partial class SampleHostWindow : Window
    {
        private IExchange baseExchange;
        private SDKOptions sdkOptions;

        public SampleHostWindow()
        {
            this.InitializeComponent();
            this.RegisterSystemLanguage();
            this.InitializeInteropBridge();
        }

        public void Destroy()
        {
            if (this.baseExchange is CustomReadWriteModel customReadWriteModel)
            {
                var exchanges = customReadWriteModel.GetLocalExchanges();
                if (exchanges != null)
                {
                    this.sdkOptions?.Storage.Add("LocalExchanges", exchanges);
                }

                this.sdkOptions?.Storage.Save();

                // Destroy interop bridge object and Connector UI
                if (customReadWriteModel.Bridge != null)
                {
                    customReadWriteModel.Bridge.SetWindowState(WindowStateEnum.Close);
                    InteropBridgeFactory.DestroyAsync(customReadWriteModel.Bridge);
                    customReadWriteModel.Bridge = null;
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

        private void InitializeInteropBridge()
        {
            var authClientId = ConfigurationManager.AppSettings["AuthClientId"];
            var authClientSecret = ConfigurationManager.AppSettings["AuthClientSecret"];
            var authCallback = ConfigurationManager.AppSettings["AuthCallback"];
            var logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var connectorName = ConfigurationManager.AppSettings["ConnectorName"];
            var connectorVersion = ConfigurationManager.AppSettings["ConnectorVersion"];
            var hostApplicationName = ConfigurationManager.AppSettings["HostApplicationName"];
            var hostApplicationVersion = ConfigurationManager.AppSettings["HostApplicationVersion"];

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

            var client = new Autodesk.DataExchange.Client(this.sdkOptions);
            var customReadWriteModel = new CustomReadWriteModel(client);
            this.baseExchange = customReadWriteModel;

            var bridgeOptions = InteropBridgeOptions.FromClient(client);
            bridgeOptions.Exchange = customReadWriteModel;
            bridgeOptions.Invoker = new MainThreadInvoker(this.Dispatcher);
            bridgeOptions.FeedbackUrl = "https://some.feedback.url";

            // At this point, the SampleHostWindow is still under construction, so the
            // WindowInteropHelper.Handle returns IntPtr.Zero. As a result, the Connector
            // UI becomes a standalone top-level window, not owned by SampleHostWindow.
            // This may cause it to appear behind the host window when focus is lost.
            //
            // In a real-world scenario, the host application's main window would be fully
            // constructed before the interop bridge is initialized, ensuring a valid Handle.
            // Therefore, this behavior is not an issue in production.
            bridgeOptions.HostWindowHandle = new WindowInteropHelper(this).Handle;

            if (this.GetLogLevel(logLevel) == LogLevel.Debug)
            {
                this.SetDebugLogLevel(this.sdkOptions?.Logger);
                this.EnableHttpLogsForDebugging(client);
            }

            customReadWriteModel.Bridge = InteropBridgeFactory.Create(bridgeOptions);

            // Subscribe to ClientStateChanged event for UI state notifications
            customReadWriteModel.Bridge.ClientStateChanged += (sender, e) =>
            {
                if (e.IsConnected)
                {
                    // Set the document name only after the Connector UI is connected.
                    // If SetDocumentName is called too early, it will have no effect
                    // because the Connector UI does not yet exist at that point.
                    customReadWriteModel.Bridge.SetDocumentName("Sample Document");
                }
            };

            this.LoadLocalExchanges(customReadWriteModel);

            // Initialize and launch the connector UI asynchronously
            _ = this.InitializeAndLaunchConnectorUi(customReadWriteModel.Bridge);
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

        private void SetDebugLogLevel(Autodesk.DataExchange.Core.Interface.ILogger logger)
        {
            logger?.SetDebugLogLevel();
        }

        private void EnableHttpLogsForDebugging(Client client)
        {
            (client as Client)?.EnableHttpDebugLogging();
        }

        private void LoadLocalExchanges(CustomReadWriteModel customReadWriteModel)
        {
            var exchanges = this.sdkOptions.Storage.Get<List<DataExchange>>("LocalExchanges");
            if (exchanges != null)
            {
                customReadWriteModel.SetLocalExchanges(exchanges);
            }
        }
    }
}
