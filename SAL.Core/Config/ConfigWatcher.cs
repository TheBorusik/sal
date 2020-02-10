using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Monad;
using SAL.Core.Helpers;

namespace SAL.Core.Config
{
    internal class EventHandlerLstItem<TKey, TEventArgs>
    {
        public TKey Key;
        public EventHandler<TEventArgs> Handler;
    }

    internal class EventHandlerList<TKey, TEventArgs>
    {
        private readonly LinkedList<EventHandlerLstItem<TKey, TEventArgs>> handlerList = new LinkedList<EventHandlerLstItem<TKey, TEventArgs>>();
        private object locker = new object();


        public EventHandler<TEventArgs> this[TKey key] => Find(key)?.Handler;


        public void AddHandler(TKey key, EventHandler<TEventArgs> value)
        {
            var e = Find(key);
            if (e != null)
            {
                lock (locker)
                    e.Handler += value;
            }
            else
            {
                lock (locker)
                    handlerList.AddLast(new EventHandlerLstItem<TKey, TEventArgs>
                    {
                        Key = key,
                        Handler = value
                    });
            }
        }

        public void RemoveHandler(TKey key, EventHandler<TEventArgs> value)
        {
            var e = Find(key);
            if (e != null)
            {
                if (value != null)
                    e.Handler -= value;
            }

        }

        private EventHandlerLstItem<TKey, TEventArgs> Find(TKey key)
        {
            var found = handlerList.First;
            while (found != null)
            {
                if (Equals(found.Value.Key,key))   
                    break;
                
                found = found.Next;
            }

            return found?.Value;
        }
    }

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

        public JObject Init(string configDir)
        {
            if (inited)
                return (JObject) configurationRoot.DeepClone();

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
            return (JObject) configurationRoot.DeepClone();
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

            Directory.EnumerateFiles(ServiceConfiguration.ConfigPath, "*.json").ForEach(fn =>
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
            return (JObject) configurationRoot.DeepClone();
        }
    }
}