using System;
using System.Collections.Generic;
using System.Globalization;
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
            return (JObject) configurationRoot.DeepClone();
        }

        public void UpdateSection(string sectionName, JObject newData)
        {
            configurationRoot.AddOrUpdate(sectionName, newData);
            configWatcher.EnableRaisingEvents = false;
            var fileCount = SectionInFiles(sectionName);
            var fileName = "";
            if (fileCount > 1)
            {
                RemoveSectionFromFiles(sectionName);
            }else if(fileCount == 1)
            {
                fileName = SectionFileName(sectionName);
            }
            
            SaveSection(sectionName, newData, fileName);
            configWatcher.EnableRaisingEvents = true;
        }

        public void MergeSection(string sectionName, JObject mergeData)
        {
            var section = configurationRoot.GetValueIC(sectionName);
            if (section == null)
            {
                configurationRoot.AddOrUpdate(sectionName, mergeData);
                section = mergeData;
            }
            else
            {
                if (section is JObject js)
                {
                    js.Merge(mergeData);
                }
                else
                {
                    section.Parent?.Remove();
                    configurationRoot.AddOrUpdate(sectionName, mergeData);
                    section = mergeData;
                }
            }
            
            configWatcher.EnableRaisingEvents = false;
            var fileCount = SectionInFiles(sectionName);
            var fileName = "";
            if (fileCount > 1)
            {
                RemoveSectionFromFiles(sectionName);
            }else if(fileCount == 1)
            {
                fileName = SectionFileName(sectionName);
            }
            

            SaveSection(sectionName, section, fileName);
            configWatcher.EnableRaisingEvents = true;
            
            
        }

        private void RemoveSectionFromFiles(string sectionName)
        {
            Directory.EnumerateFiles(AdapterConfiguration.ConfigPath, "*.json").ForEach(fn =>
            {
                try
                {
                    var configData = File.ReadAllText(fn);
                    var jData = JObject.Parse(configData);
                    var section = jData.GetValueIC(sectionName);
                    if (section != null)
                    { 
                        section.Parent?.Remove();
                        File.WriteAllText(fn, jData.ToIndentedJson());
                    }
                    
                }
                catch (Exception ex)
                {
                    //
                }
            });

        }

        private int SectionInFiles(string sectionName)
        {
            var files = 0;
            Directory.EnumerateFiles(AdapterConfiguration.ConfigPath, "*.json").ForEach(fn =>
            {
                try
                {
                    var configData = File.ReadAllText(fn);
                    var jData = JToken.Parse(configData);
                    var section = jData.GetValueIC(sectionName);
                    if (section != null)
                    {
                        files++;
                    }
                }
                catch (Exception ex)
                {
                    //
                }
            });
            return files;
        }

        private string SectionFileName(string sectionName)
        {
            var filename = "";
            Directory.EnumerateFiles(AdapterConfiguration.ConfigPath, "*.json").ForEach(fn =>
            {
                try
                {
                    var configData = File.ReadAllText(fn);
                    var jData = JToken.Parse(configData);
                    var section = jData.GetValueIC(sectionName);
                    if (section != null)
                    {
                        filename = fn;
                    }
                }
                catch (Exception ex)
                {
                    //
                }
            });
            return filename;
        }

        private void SaveSection(string sectionName, JToken sectionData, string filename)
        {          
            var fileName = Path.Combine(AdapterConfiguration.ConfigPath, sectionName + ".json");
            if (!string.IsNullOrEmpty(filename))
                fileName = filename;
            var fileData = new JObject();
            if (File.Exists(fileName))
            {
                try
                {
                    var configData = File.ReadAllText(fileName);
                    fileData = JObject.Parse(configData);
                    var section = fileData.GetValueIC(sectionName);
                    if (section != null)
                    {
                        section.Parent?.Remove();
                    }
                }
                catch (Exception ex)
                {
                    fileData = new JObject();
                }
            }
            fileData.Add(sectionName, sectionData);
            File.WriteAllText(fileName, fileData.ToIndentedJson());
        }
        
    }
}