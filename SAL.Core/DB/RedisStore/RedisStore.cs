using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.Configuration.Redis;
using StackExchange.Redis;
using When = SAL.API.When;

namespace SAL.Core.DB.RedisStore
{
    public class RedisStore : IRedisStore, IDisposable
    {
        private ILogger logger;
        private ConnectionMultiplexer redis;
        private IDatabase datebase;

        public RedisStore(ILoggerProvider loggerProvider, IConfigWatcher configWatcher)
        {
            logger = loggerProvider.CreateLogger("RedisStore");
            var config = configWatcher.GetSection(ConfigurationSectionNames.RedisStore).ConvertValue<RedisStoreConfig>();
            
            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = config.Redis.AbortOnConnectFail,
                AllowAdmin = config.Redis.AllowAdmin,
                ChannelPrefix = config.Redis.ChannelPrefix,
                CheckCertificateRevocation = config.Redis.CheckCertificateRevocation,
                ConnectRetry = config.Redis.ConnectRetry,
                ConnectTimeout = config.Redis.ConnectTimeout,
                DefaultDatabase = config.Redis.DefaultDatabase,
                KeepAlive = config.Redis.KeepAlive,
                ClientName = config.Redis.ClientName,
                Password = config.Redis.Password,
                User = config.Redis.User,
                ServiceName = config.Redis.ServiceName,
                ResolveDns = config.Redis.ResolveDns,
                Ssl = config.Redis.Ssl,
                SslHost = config.Redis.SslHost,
                SslProtocols = config.Redis.SslProtocols,
                SyncTimeout = config.Redis.SyncTimeout,
                AsyncTimeout = config.Redis.AsyncTimeout
            };
            foreach(var redisEndpoint in config.Redis.Endpoints)
            {
                options.EndPoints.TryAdd(new DnsEndPoint(redisEndpoint.Host, redisEndpoint.Port));
            }
            
            redis = ConnectionMultiplexer.Connect(options);
        }

        private RedisStore(ILogger logger, IDatabase datebase)
        {
            this.logger = logger;
            this.datebase = datebase;
        }



        private IDatabase GetDB()
        {
            return datebase ?? redis.GetDatabase();
        }


        public Task<bool> Add(string key, object value)
        {
            var db = GetDB();
            return db.StringSetAsync(key, value.ToJson());
        }

        public Task<bool> TryPeek<T>(string key, out T value)
        {
            value = default;

            var db = GetDB();
            ;
            var redisVal = db.StringGet(key);

            if (redisVal.IsNullOrEmpty)
                return Task.FromResult(false);
            try
            {
                value = Convert<T>(redisVal);
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> TryTake<T>(string key, out T value)
        {
            value = default;

            var db = GetDB();
            var redisVal = db.StringGet(key);

            if (redisVal.IsNullOrEmpty)
                return Task.FromResult(false);

            try
            {
                value = Convert<T>(redisVal);
                return Task.FromResult(db.KeyDelete(key));
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
            
        }

        public Task<bool> TryPeekRaw(string key, out string value)
        {
            value = default;

            var db = GetDB();
            var redisVal = db.StringGet(key);

            if (redisVal.IsNullOrEmpty)
                return Task.FromResult(false);

            value = redisVal;
            return Task.FromResult(true);
        }

        public Task<bool> TryTakeRaw(string key, out string value)
        {
            value = default;

            var db = GetDB();
            var redisVal = db.StringGet(key);

            if (redisVal.IsNullOrEmpty)
                return Task.FromResult(false);

            value = redisVal;
            return Task.FromResult(db.KeyDelete(key));
        }

        private T Convert<T>(RedisValue value)
        {
            return ((string) value).ConvertValue<T>();
        }

        public IRedisStore GetDatebase()
        {
            if (redis == null)
                throw new NotSupportedException("");

            return new RedisStore(logger, redis.GetDatabase());
        }

        public bool KeyDelete(string key)
        {
            var db = GetDB();
            return db.KeyDelete(key);
        }

        public Task<bool> KeyDeleteAsync(string key)
        {
            var db = GetDB();
            return db.KeyDeleteAsync(key);
        }

        public long KeyDelete(string[] keys)
        {
            var db = GetDB();
            return db.KeyDelete(keys.Select(s => new RedisKey(s)).ToArray());
        }

        public Task<long> KeyDeleteAsync(string[] keys)
        {
            var db = GetDB();
            return db.KeyDeleteAsync(keys.Select(s => new RedisKey(s)).ToArray());
        }

        public bool KeyExists(string key)
        {
            var db = GetDB();
            return db.KeyExists(key);
        }

        public Task<bool> KeyExistsAsync(string key)
        {
            var db = GetDB();
            return db.KeyExistsAsync(key);
        }

        public long KeyExists(string[] keys)
        {
            var db = GetDB();
            return db.KeyExists(keys.Select(s => new RedisKey(s)).ToArray());
        }

        public Task<long> KeyExistsAsync(string[] keys)
        {
            var db = GetDB();
            return db.KeyExistsAsync(keys.Select(s => new RedisKey(s)).ToArray());
        }

        public bool KeyExpire(string key, TimeSpan expiry)
        {
            var db = GetDB();
            return db.KeyExpire(key, expiry);
        }

        public Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            var db = GetDB();
            return db.KeyExpireAsync(key, expiry);
        }

        public bool KeyExpire(string key, DateTime expiry)
        {
            var db = GetDB();
            return db.KeyExpire(key, expiry);
        }

        public Task<bool> KeyExpireAsync(string key, DateTime expiry)
        {
            var db = GetDB();
            return db.KeyExpireAsync(key, expiry);
        }

        public TimeSpan? KeyIdleTime(string key)
        {
            var db = GetDB();
            return db.KeyIdleTime(key);
        }

        public Task<TimeSpan?> KeyIdleTimeAsync(string key)
        {
            var db = GetDB();
            return db.KeyIdleTimeAsync(key);
        }

        public bool KeyPersist(string key)
        {
            var db = GetDB();
            return db.KeyPersist(key);
        }

        public Task<bool> KeyPersistAsync(string key)
        {
            var db = GetDB();
            return db.KeyPersistAsync(key);
        }

        public bool KeyRename(string key, string newKey)
        {
            var db = GetDB();
            return db.KeyRename(key, newKey);
        }

        public Task<bool> KeyRenameAsync(string key, string newKey)
        {
            var db = GetDB();
            return db.KeyRenameAsync(key, newKey);
        }

        public TimeSpan? KeyTimeToLive(string key)
        {
            var db = GetDB();
            return db.KeyTimeToLive(key);
        }

        public Task<TimeSpan?> KeyTimeToLiveAsync(string key)
        {
            var db = GetDB();
            return db.KeyTimeToLiveAsync(key);
        }

        public Task<bool> Add(string key, object value, TimeSpan? expiry = null, When when = When.Always)
        {
            var db = GetDB();
            return db.StringSetAsync(key, value.ToJson(), expiry, ToRedis(when));
        }



        public bool AddString(string key, string value, TimeSpan? expiry = null, When when = When.Always)
        {
            var db = GetDB();
            return db.StringSet(key, value, expiry, ToRedis(when));
        }

        public Task<bool> AddStringAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always)
        {
            var db = GetDB();
            return db.StringSetAsync(key, value, expiry, ToRedis(when));
        }

        public long StringLength(string key)
        {
            var db = GetDB();
            return db.StringLength(key);
        }

        public Task<long> StringLengthAsync(string key)
        {
            var db = GetDB();
            return db.StringLengthAsync(key);
        }

        public long StringIncrement(string key, long value = 1)
        {
            var db = GetDB();
            return db.StringIncrement(key,value);
        }

        public Task<long> StringIncrementAsync(string key, long value = 1)
        {
            var db = GetDB();
            return db.StringIncrementAsync(key, value);
        }

        public double StringIncrement(string key, double value)
        {
            var db = GetDB();
            return db.StringIncrement(key, value);
        }

        public Task<double> StringIncrementAsync(string key, double value)
        {
            var db = GetDB();
            return db.StringIncrementAsync(key, value);
        }

        public double StringDecrement(string key, double value)
        {
            var db = GetDB();
            return db.StringDecrement(key, value);
        }

        public Task<double> StringDecrementAsync(string key, double value)
        {
            var db = GetDB();
            return db.StringDecrementAsync(key, value);
        }

        public long StringDecrement(string key, long value = 1)
        {
            var db = GetDB();
            return db.StringDecrement(key, value);
        }

        public Task<long> StringDecrementAsync(string key, long value = 1)
        {
            var db = GetDB();
            return db.StringDecrementAsync(key, value);
        }

        public string GetString(string key)
        {
            var db = GetDB();
            return db.StringGet(key);
        }

        public async Task<string> GetStringAsync(string key)
        {
            var db = GetDB();
            var redisValue =  await db.StringGetAsync(key);
            return redisValue;
        }


        private StackExchange.Redis.When ToRedis(When @when)
        {
            return @when switch
            {
                When.Always => StackExchange.Redis.When.Always,
                When.Exists => StackExchange.Redis.When.Exists,
                When.NotExists => StackExchange.Redis.When.NotExists,
                _ => throw new ArgumentOutOfRangeException(nameof(when), @when, null)
            };
        }
        public void Dispose()
        {
            redis?.Dispose();
        }
    }
}