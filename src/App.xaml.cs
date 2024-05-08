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
using System.Reflection;
using System.IO;

namespace SampleConnector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IExchange baseExchange;
        private SDKOptions _sdkOptions;
        internal static string _installationPath;
        private AppDomain _appDomain;

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

            var connectorInstallationDir = new System.Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            _installationPath = Path.GetDirectoryName(connectorInstallationDir);
            _appDomain = AppDomain.CurrentDomain;
            _appDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

            _sdkOptions = new SDKOptionsDefaultSetup()
            {
                ApplicationName = applicationName,
                ClientId = authClientID,
                ClientSecret = authClientSecret,
                CallBack = authCallBack,
                ConnectorName = applicationName,
                ConnectorVersion = "1.0.0",
                ApplicationProductId = "Dummy",
                ApplicationVersion = "1.0",
            };

            Client client = new Autodesk.DataExchange.Client(_sdkOptions);
            
            CustomReadWriteModel customReadWriteModel = new CustomReadWriteModel(client);
            baseExchange = customReadWriteModel;

            Autodesk.DataExchange.UI.Configuration uiConfiguration = new Autodesk.DataExchange.UI.Configuration();

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
        /// <summary>
        /// Assembly resolve event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;
            try
            {
                var name = GetAssemblyName(args);
                var dllPath = Path.Combine(_installationPath, name + ".dll");
                if (File.Exists(dllPath))
                {
                    _sdkOptions.Logger?.Debug("Loading assembly " + args.Name);
                    assembly = Assembly.LoadFile(dllPath);
                }

            }
            catch (Exception e)
            {
                _sdkOptions.Logger?.Debug("Failed to load assembly " + args.Name);
                _sdkOptions.Logger?.Error(e);
            }
            return assembly;
        }

        /// <summary>
        /// Get assembly name
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string GetAssemblyName(ResolveEventArgs args)
        {
            string name;
            if (args.Name.IndexOf(",") > -1)
            {
                name = args.Name.Substring(0, args.Name.IndexOf(","));
            }
            else
            {
                name = args.Name;
            }
            return name;
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
