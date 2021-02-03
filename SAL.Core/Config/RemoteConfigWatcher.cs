using System;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Events;

namespace SAL.Core.Config
{
    public class RemoteConfigWatcher : IConfigWatcher
        //,IEventHandler<>
    {
        private JObject configurationRoot;
        private readonly JsonMergeSettings mergeSettings;
        private bool inited = false;
        
        private readonly EventHandlerList<string, ConfigurationSectionChangedEventArgs> listEventDelegates = new EventHandlerList<string, ConfigurationSectionChangedEventArgs>();

        
        public void Subscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler)
        {
            listEventDelegates.AddHandler(sectionName.ToLower(), handler);
        }

        public void UnSubscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler)
        {
            listEventDelegates.RemoveHandler(sectionName.ToLower(), handler);
        }

        public JToken GetSection(string sectionName)
        {
            return configurationRoot.GetValueIC(sectionName)?.DeepClone();
        }

        public JObject GetConfig()
        {
            return configurationRoot.Clone();
        }


        public JObject Init()
        {
            return null;
        }

        [Obsolete]
        public void UpdateSection(string sectionName, JObject newData)
        {
            
        }

        [Obsolete]
        public void MergeSection(string sectionName, JObject mergeData)
        {
        }

        public void Start()
        {
        }

        public void Online()
        {
        }

        public void Offline()
        {
        }

        public void Stop()
        {
        }
    }
}