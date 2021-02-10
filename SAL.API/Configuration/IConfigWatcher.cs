using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface IConfigWatcher
    {
       //event EventHandler<ConfigurationSectionChangedEventArgs> SectionChanged;
        void Subscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler);
        void UnSubscribe(string sectionName, EventHandler<ConfigurationSectionChangedEventArgs> handler);

        JToken GetSection(string sectionName);
        JObject GetConfig();
        
        void UpdateSection(string sectionName, JObject newData);
        void MergeSection(string sectionName, JObject mergeData);

        bool CanUpdateConfig();
    }
}