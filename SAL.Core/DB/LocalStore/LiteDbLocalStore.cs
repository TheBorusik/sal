using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json.Linq;
using SAL.API;

namespace SAL.Core.DB.LocalStore
{
    internal class KeyValue
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }


    [Obsolete]
    public class LiteDbLocalStore : ILocalStore, IDisposable
    {
        private LiteDatabase db;
        private ILiteCollection<KeyValue> collection;

        public LiteDbLocalStore()
        {
            db = new LiteDatabase(Path.Combine(AdapterConfiguration.DiskStorePath, "localStore.db"));
            collection = db.GetCollection<KeyValue>("local");
            collection.EnsureIndex(x => x.Id);
        }


        public Task<bool> Add(string key, object value)
        {
            if (collection == null)
                return Task.FromResult(false);

            return Task.FromResult(collection.Upsert(new KeyValue
            {
                Id = key,
                Value = value.ToJson()
            }));
        }

        public Task<bool> TryPeek<T>(string key, out T value)
        {
            value = default;
            if (TryPeekRaw(key, out var str).Result)
            {
                try
                {
                    value = Convert<T>(str);
                    
                    return Task.FromResult(true);
                }
                catch (Exception)
                {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false);
        }

        public Task<bool> TryTake<T>(string key, out T value)
        {
            value = default;
            if (TryTakeRaw(key, out var str).Result)
            {
                try
                {
                    value = Convert<T>(str);
                    return Task.FromResult(true);
                }
                catch (Exception)
                {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false);
        }

        public Task<bool> TryPeekRaw(string key, out string value)
        {
            value = null;
            if (collection == null)
                return Task.FromResult(false);

            var kv = collection.FindById(key);
            if (kv == null)
                return Task.FromResult(false);

            value = kv.Value;
            return Task.FromResult(true);

        }

        public Task<bool> TryTakeRaw(string key, out string value)
        {
            var result = TryPeekRaw(key, out value).Result;
            if(result)
                collection.Delete(key);
            return Task.FromResult(result);
        }

        private T Convert<T>(string value)
        {
            return value.ConvertValue<T>();
        }


        public void Dispose()
        {
            db?.Dispose();
            db = null;
            collection = null;
        }
    }
}