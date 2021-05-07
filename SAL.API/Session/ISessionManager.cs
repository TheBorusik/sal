using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ISessionManager
    {
        Session GetCurrent();

        void Update(API.Session session);
        void Refresh(API.Session session);



    }
    
    public class Session 
    {
        public bool IsLocal { get; internal set; }
        public bool IsChanged { get; internal set; }
        
        public string SessionId { get; internal set; }
        public long? AuthId { get; internal set; }
        public long? ProcessId { get; internal set; }

        internal JObject data;
        private ISessionManager manager;

        internal Session(ISessionManager manager)
        {
            this.manager = manager;
        }


        public JObject GetRawData()
        {
            return data.Clone();
        }

        public void AddOrUpdate(string key, object value)
        {
            data.AddOrUpdate(key, value);
            IsChanged = true;
        }

        public void Remove(string key)
        {            
            if(data.Remove(key))
                IsChanged = true;
        }

        public T GetValue<T>(string key)
        {
            return data.GetValue<T>(key);
        }
        
        public bool TryGetValue<T>(string key, out T value)
        {
            return data.TryGetValue(key, out value);
        }
        
        public T GetSafeValue<T>(string key, T defaultValue = default)
        {
            return data.GetSafeValue(key, defaultValue);
        }

        public void Update()
        {
            if (IsChanged)
            {
                manager.Update(this);
            }
        }

        public void Refresh()
        {
            manager.Refresh(this);

        }





    }
}