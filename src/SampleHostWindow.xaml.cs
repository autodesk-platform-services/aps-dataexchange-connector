using Autodesk.DataExchange;
using Autodesk.DataExchange.BaseModels;
using Autodesk.DataExchange.Core;
using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.Core.Interface;
using Autodesk.DataExchange.Core.Models;
using Autodesk.DataExchange.UI.Core;
using Autodesk.DataExchange.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace SampleConnector
{
    // TODO: Remove this CustomReadWriteModel class later.
    public class CustomReadWriteModel : BaseReadWriteExchangeModel
    {
        public IInteropBridge interopBridge;
        private List<DataExchange> localStorage = new List<DataExchange>();

        public CustomReadWriteModel(Client client) : base(client)
        {
        }

        public override List<DataExchange> GetCachedExchanges()
        {
            return new List<DataExchange>();
        }

        public override Task<List<DataExchange>> GetExchangesAsync(ExchangeSearchFilter exchangeSearchFilter)
        {
            return Task.FromResult(new List<DataExchange>());
        }

        public override Task<bool> SelectElementsAsync(List<string> exchangeIds)
        {
            return Task.FromResult(false);
        }

        public override Task<IEnumerable<string>> UnloadExchangesAsync(List<ExchangeItem> exchanges)
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public override Task UpdateExchangeAsync(ExchangeItem exchangeItem, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        internal List<DataExchange> GetLocalExchanges()
        {
            return localStorage?.ToList();
        }

        internal void SetLocalExchanges(List<DataExchange> dataExchanges)
        {
            localStorage.AddRange(dataExchanges);
        }
    }

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
                if (customReadWriteModel.interopBridge != null)
                {
                    InteropBridgeFactory.DestroyAsync(customReadWriteModel.interopBridge).Wait();
                    customReadWriteModel.interopBridge = null;
                }
            }
        }

        private void RegisterSystemLanguage()
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
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

            if (this.GetLogLevel(logLevel) == LogLevel.Debug)
            {
                this.SetDebugLogLevel(this.sdkOptions?.Logger);
                this.EnableHttpLogsForDebugging(client);
            }

            customReadWriteModel.interopBridge = InteropBridgeFactory.Create(bridgeOptions);
            this.LoadLocalExchanges(customReadWriteModel);

            // Launch the connector UI asynchronously
            _ = this.LaunchConnectorUi();
        }

        private async Task LaunchConnectorUi()
        {
            // Add connector UI launching logic here
            await Task.CompletedTask;
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
