using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Monad;
using SAL.Core.Helpers;

namespace SAL.Core.Config
{
    public class ConfigWatcher : IConfigWatcher
    {
        private readonly FileSystemWatcher configWatcher = new FileSystemWatcher();
        private JObject configurationRoot;
        private readonly JsonMergeSettings mergeSettings;
        private bool inited = false;



        private readonly EventHandlerList<string, ConfigurationSectionChangedEventArgs> listEventDelegates = new EventHandlerList<string, ConfigurationSectionChangedEventArgs>();
/*        public event EventHandler<ConfigurationSectionChangedEventArgs> SectionChanged
        {
            add => listEventDelegates.AddHandler("", value);
            remove => listEventDelegates.RemoveHandler("", value);
        }*/

        public void Subscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler)
        {
            listEventDelegates.AddHandler(sectionName.ToLower(), handler);
        }

        public void UnSubscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler)
        {
            listEventDelegates.RemoveHandler(sectionName.ToLower(), handler);
        }


        public ConfigWatcher()
        {
            mergeSettings = new JsonMergeSettings
            {
                MergeNullValueHandling = MergeNullValueHandling.Ignore,
                MergeArrayHandling = MergeArrayHandling.Union
            };
        }

        public void Init(string configDir)
        {
            if (inited)
                return;

            configurationRoot = MakeConfig();

            configWatcher.Path = configDir;

            configWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            configWatcher.Filter = "*.json";

            configWatcher.Changed += ConfigOnChanged;
            configWatcher.Created += ConfigOnChanged;
            configWatcher.Deleted += ConfigOnChanged;
            configWatcher.Renamed += ConfigOnChanged;

            configWatcher.EnableRaisingEvents = true;
            inited = true;
        }
        
        public void Done()
        {
            configWatcher?.Dispose();
        }

        private void ReadConfigurationFile(string fileName, JObject config)
        {
            try
            {
                var configData = File.ReadAllText(fileName);
                var jData = JToken.Parse(configData);
                config.Merge(jData, mergeSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошика чтения файла {fileName} ", ex);
            }
        }

        private void ConfigOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            configWatcher.EnableRaisingEvents = false;

            var tmpConfig = MakeConfig();

            var diff = JsonPatcher.Diff(configurationRoot, tmpConfig);

            configurationRoot = JsonPatcher.Patch(configurationRoot, diff);

            diff.ForEach(d =>
            {
                if (d is JProperty dp)
                {
                    var evnt = new ConfigurationSectionChangedEventArgs(dp.Name);
                    var eventDelegate = listEventDelegates[dp.Name.ToLower()]; // as EventHandler<ConfigurationSectionChangedEventArgs>;
                    eventDelegate?.Invoke(this, evnt);
                    eventDelegate = listEventDelegates[""];
                    eventDelegate?.Invoke(this, evnt);
                }
            });

            configWatcher.EnableRaisingEvents = true;
        }

        private JObject MakeConfig()
        {
            var config = new JObject();

            Directory.EnumerateFiles(AdapterConfiguration.ConfigPath, "*.json").ForEach(fn =>
            {
                if (fn.EndsWith("service.json", StringComparison.InvariantCultureIgnoreCase))
                    return;
                ReadConfigurationFile(fn, config);
            });

            return config;
        }

        public JToken GetSection(string sectionName)
        {
            return configurationRoot.GetValueIC(sectionName)?.DeepClone();
        }

        public JObject GetConfig()
        {
            return configurationRoot.Clone();
        }
        
    }
}