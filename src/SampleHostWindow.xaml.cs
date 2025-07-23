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
            throw new NotImplementedException();
        }

        public override Task<List<DataExchange>> GetExchangesAsync(ExchangeSearchFilter exchangeSearchFilter)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> SelectElementsAsync(List<string> exchangeIds)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<string>> UnloadExchangesAsync(List<ExchangeItem> exchanges)
        {
            throw new NotImplementedException();
        }

        public override Task UpdateExchangeAsync(ExchangeItem exchangeItem, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public List<DataExchange> GetLocalExchanges()
        {
            return localStorage?.ToList();
        }

        public void SetLocalExchanges(List<DataExchange> dataExchanges)
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
            string authClientID = ConfigurationManager.AppSettings["AuthClientID"];
            var authClientSecret = ConfigurationManager.AppSettings["AuthClientSecret"];
            var authCallBack = ConfigurationManager.AppSettings["AuthCallBack"];

            var logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            if (string.IsNullOrEmpty(applicationName))
                applicationName = "SampleConnector";

            this.sdkOptions = new SDKOptionsDefaultSetup()
            {
                HostApplicationName = applicationName,
                ClientId = authClientID,
                ClientSecret = authClientSecret,
                CallBack = authCallBack,
                ConnectorName = applicationName,
                ConnectorVersion = "1.0.0",
                HostApplicationVersion = "1.0",
            };

            var client = new Autodesk.DataExchange.Client(this.sdkOptions);
            var customReadWriteModel = new CustomReadWriteModel(client);
            this.baseExchange = customReadWriteModel;

            var bridgeOptions = InteropBridgeOptions.FromClient(client);
            bridgeOptions.Exchange = customReadWriteModel;

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
