using Autodesk.DataExchange.Core.Interface;
using Autodesk.DataExchange;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Autodesk.DataExchange.Authentication;
using Autodesk.DataExchange.UI.ViewModels.Models;
using System.Windows.Interop;
using Autodesk.DataExchange.Core.Models;
using ILogger = Autodesk.DataExchange.Core.Interface.ILogger;
using Autodesk.DataExchange.Extensions.HostingProvider;
using Autodesk.DataExchange.Core.Enums;
using Autodesk.DataExchange.Models;

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
            
            string logLevel = ConfigurationManager.AppSettings?["LogLevel"];
            var appBasePath = ConfigurationManager.AppSettings["ApplicationDataPath"];
            var applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            if (string.IsNullOrEmpty(applicationName))
                applicationName = "SampleConnector";

            if (string.IsNullOrEmpty(appBasePath))
                appBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            appBasePath = Path.Combine(appBasePath, applicationName + "-Connector");

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

            uiConfiguration.LogLevel = Autodesk.DataExchange.Core.Enums.LogLevel.Debug;
            if (uiConfiguration.LogLevel == Autodesk.DataExchange.Core.Enums.LogLevel.Debug)
                SetDebugLogLevel(_sdkOptions?.Logger);
            
            var application = new Autodesk.DataExchange.UI.Application(customReadWriteModel, uiConfiguration);
            customReadWriteModel.Application = application;
            application.AdvanceLoadExchangeEvent += AppManager_AdvanceLoadExchangeEvent;
            LoadLocalExchanges(customReadWriteModel);
            application.Show();
        }

        private void SetDebugLogLevel(Autodesk.DataExchange.Core.Interface.ILogger logger)
        {
            logger?.SetDebugLogLevel();
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
