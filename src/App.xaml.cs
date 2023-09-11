using Autodesk.DataExchange.Core.Interface;
using Autodesk.DataExchange;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Autodesk.DataExchange.Core.Models;
using Autodesk.DataExchange.Core.Enums;

namespace SampleConnector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IExchange baseExchange;
        private SDKOptions _sdkOptions;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            RegisterSystemLanguage();
            StartConnector();
        }
        private void StartConnector()
        {
            string authClientID = ConfigurationManager.AppSettings["AuthClientID"];
            var authClientSecret = ConfigurationManager.AppSettings["AuthClientSecret"];
            var authCallBack = ConfigurationManager.AppSettings["AuthCallBack"];
            
            var logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            if (string.IsNullOrEmpty(applicationName))
                applicationName = "SampleConnector";

            _sdkOptions = new SDKOptionsDefaultSetup()
            {
                ApplicationName = applicationName,
                ClientId = authClientID,
                ClientSecret = authClientSecret,
                CallBack = authCallBack,
            };

            Client client = new Autodesk.DataExchange.Client(_sdkOptions);
            
            CustomReadWriteModel customReadWriteModel = new CustomReadWriteModel(client);
            baseExchange = customReadWriteModel;

            Autodesk.DataExchange.UI.Configuration uiConfiguration = new Autodesk.DataExchange.UI.Configuration();
            uiConfiguration.ConnectorVersion = "1.0.0";
            uiConfiguration.HostingProductID = "Dummy";
            uiConfiguration.HostingProductVersion = "1.0";

            uiConfiguration.LogLevel = GetLogLevel(logLevel);
            if (uiConfiguration.LogLevel == Autodesk.DataExchange.Core.Enums.LogLevel.Debug)
            {
                SetDebugLogLevel(_sdkOptions?.Logger);
                EnableHttpLogsForDebugging(client);
            }
            
            var application = new Autodesk.DataExchange.UI.Application(customReadWriteModel, uiConfiguration);
            customReadWriteModel.Application = application;
            application.AdvanceLoadExchangeEvent += AppManager_AdvanceLoadExchangeEvent;
            LoadLocalExchanges(customReadWriteModel);
            application.Show();
        }

        private LogLevel GetLogLevel(string logLevel)
        {
            LogLevel parsedlogLevel;
            bool canConvertToEnum =  Enum.TryParse<LogLevel>(logLevel, true, out parsedlogLevel);

            if(canConvertToEnum)
                return parsedlogLevel;
            else return LogLevel.Error;
        }

        private void SetDebugLogLevel(Autodesk.DataExchange.Core.Interface.ILogger logger)
        {
            logger?.SetDebugLogLevel();
        }

        private void EnableHttpLogsForDebugging(Client client)
        {
            (client as Client)?.EnableHttpDebugLogging();
        }

        private void AppManager_AdvanceLoadExchangeEvent(object obj)
        {
            
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

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (baseExchange is CustomReadWriteModel customReadWriteModel)
            {
                var exchanges = customReadWriteModel.GetLocalExchanges();
                if (exchanges != null)
                {
                    _sdkOptions?.Storage.Add("LocalExchanges", exchanges);
                }
                _sdkOptions?.Storage.Save();
            }
        }

        private void LoadLocalExchanges(CustomReadWriteModel customReadWriteModel)
        {
            var exchanges = _sdkOptions.Storage.Get<List<DataExchange>>("LocalExchanges");
            if (exchanges != null)
            {
                customReadWriteModel.SetLocalExchanges(exchanges);                
           }
        }
    }
}
