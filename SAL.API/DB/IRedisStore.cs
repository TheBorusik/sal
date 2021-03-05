using System;
using System.Threading.Tasks;

namespace SAL.API
{
    public interface IRedisStore : IStore
    {

        IRedisStore GetDatebase();
        
        bool KeyDelete(string key);
        Task<bool> KeyDeleteAsync(string key);
        long KeyDelete(string[] keys);
        Task<long> KeyDeleteAsync(string[] keys);
        
        bool KeyExists(string key);
        Task<bool> KeyExistsAsync(string key);
        long KeyExists(string[] keys);
        Task<long> KeyExistsAsync(string[] keys);
        
        bool KeyExpire(string key, TimeSpan expiry);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        bool KeyExpire(string key, DateTime expiry);
        Task<bool> KeyExpireAsync(string key, DateTime expiry);
        
        TimeSpan? KeyIdleTime(string key);
        Task<TimeSpan?> KeyIdleTimeAsync(string key);
        
        bool KeyPersist(string key);
        Task<bool> KeyPersistAsync(string key);
        
        bool KeyRename(string key, string newKey);
        Task<bool> KeyRenameAsync(string key, string newKey);
        
        TimeSpan? KeyTimeToLive(string key);
        Task<TimeSpan?> KeyTimeToLiveAsync(string key);
        
        
        Task<bool> Add(string key, object value,TimeSpan? expiry = null, When when = When.Always);
        bool AddString(string key, string value, TimeSpan? expiry = null, When when = When.Always);
        Task<bool> AddStringAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always);
        
        long StringLength(string key);
        Task<long> StringLengthAsync(string key);
        
        long StringIncrement(string key, long value = 1);
        Task<long> StringIncrementAsync(string key, long value = 1);
        double StringIncrement(string key, double value);
        Task<double> StringIncrementAsync(string key, double value);
        
        double StringDecrement(string key, double value);
        Task<double> StringDecrementAsync(string key, double value);
        long StringDecrement(string key, long value = 1);
        Task<long> StringDecrementAsync(string key, long value = 1);

    }
    
    public enum When
    {
        Always,
        Exists,
        NotExists,
    }
}