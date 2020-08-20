using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Common;
using SAL.API;

namespace SAL.Core.NLogEx.Adapter
{
    public class NLogFactoryAdapter  
    {
        public readonly LogFactory logFactory;

        public NLogFactoryAdapter(JToken nlogConfig)
        {
            logFactory = new LogFactory();
            Load(nlogConfig);

        }

        private void Load(JToken nlogConfig)
        {
            logFactory.Configuration = new JTokenLoggingConfiguration(nlogConfig, logFactory);

            logFactory.KeepVariablesOnReload = true;

            logFactory.Configuration.Variables["serviceName"] = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}";
            logFactory.Configuration.Variables["serviceContour"] = AdapterConfiguration.Contour;

            var logDir = Path.Combine(AdapterConfiguration.LogRootPath, $"{AdapterConfiguration.Contour}-{ AdapterConfiguration.AdapterType}.{ AdapterConfiguration.AdapterName}");
            logFactory.Configuration.Variables["logDir"] = logDir;


            logFactory.ReconfigExistingLoggers();
        }

        public void Reload(JToken nlogConfig)
        {
            if (logFactory.Configuration is JTokenLoggingConfiguration j && j.AutoReload)
            {
                try
                {
                    var tmpLogFactory = new LogFactory();
                    tmpLogFactory.Configuration = new JTokenLoggingConfiguration(nlogConfig, tmpLogFactory);

                    Load(nlogConfig);

                }
                catch (Exception ex)
                {
                    InternalLogger.Error(ex, "При обновлении конфигурации NLog произошла ошибка: ");
                }
            }
        }


        public Logger GetLogger(string name)
        {
            return logFactory.GetLogger(name);
        }

        public T GetLogger<T>(string name) where T : Logger
        {
            return logFactory.GetLogger<T>(name);
        }

        public Logger GetLogger(string name, Type loggerType)
        {
            return logFactory.GetLogger(name, loggerType);
        }
    }

}
