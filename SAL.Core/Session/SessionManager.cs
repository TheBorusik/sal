using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Configuration.Redis;
using StackExchange.Redis;

namespace SAL.Core.Session
{
    public class SessionManager : ISessionManager
    {
        
        private ILogger logger;
        private ConnectionMultiplexer redis;
        
        public SessionManager(ILoggerProvider loggerProvider, IConfigWatcher configWatcher)
        {
            logger = loggerProvider.CreateLogger("SessionManager");
            var redisConfig = configWatcher.GetSection(ConfigurationSectionNames.RedisStore).ConvertValue<RedisStoreConfig>();
            
            
            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = redisConfig.Redis.AbortOnConnectFail,
                AllowAdmin = redisConfig.Redis.AllowAdmin,
                ChannelPrefix = redisConfig.Redis.ChannelPrefix,
                CheckCertificateRevocation = redisConfig.Redis.CheckCertificateRevocation,
                ConnectRetry = redisConfig.Redis.ConnectRetry,
                ConnectTimeout = redisConfig.Redis.ConnectTimeout,
                DefaultDatabase = redisConfig.SessionDatabase ?? redisConfig.Redis.DefaultDatabase,
                KeepAlive = redisConfig.Redis.KeepAlive,
                ClientName = redisConfig.Redis.ClientName,
                Password = redisConfig.Redis.Password,
                User = redisConfig.Redis.User,
                ServiceName = redisConfig.Redis.ServiceName,
                ResolveDns = redisConfig.Redis.ResolveDns,
                Ssl = redisConfig.Redis.Ssl,
                SslHost = redisConfig.Redis.SslHost,
                SslProtocols = redisConfig.Redis.SslProtocols,
                SyncTimeout = redisConfig.Redis.SyncTimeout,
                AsyncTimeout = redisConfig.Redis.AsyncTimeout
            };
            foreach(var redisEndpoint in redisConfig.Redis.Endpoints)
            {
                options.EndPoints.TryAdd(new DnsEndPoint(redisEndpoint.Host, redisEndpoint.Port));
            }
            
            redis = ConnectionMultiplexer.Connect(options);
            
        }

        public Session GetCurrent()
        {
            var sessionId = HandlerContext.SessionId;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var db = redis.GetDatabase();
                if (db.KeyExists(sessionId))
                {
                    var sessionData = db.StringGet(sessionId);
                    try
                    {
                        var sessioinData = JObject.Parse(sessionData);
                        return new Session(this)
                        {
                            SessionId = sessionId,
                            AuthId = HandlerContext.AuthId,
                            ProcessId = HandlerContext.ProcessId,
                            IsLocal = false,
                            IsChanged = false,
                            data = sessioinData
                        };

                    }
                    catch (Exception ex)
                    {
                        logger.Error("SessionDataError",ex);
                    }
                }
            }
            

            return new Session(this)
            {
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                IsLocal = true,
                data = new JObject()
            };
            
        }
        
        
        public void Update(Session session)
        {
            if(session.IsLocal)
                return;
            var db = redis.GetDatabase();
            if (db.KeyExists(session.SessionId))
            {
                var ttl = db.KeyTimeToLive(session.SessionId);
                db.StringSet(session.SessionId, session.data.ToJson(), ttl);
                session.IsChanged = false;
            }
        }

        public void Refresh(Session session)
        {
            if(session.IsLocal)
                return;
            var db = redis.GetDatabase();
            if (db.KeyExists(session.SessionId))
            {

                var sessionData = db.StringGet(session.SessionId);
                try
                {
                    session.data = JObject.Parse(sessionData);
                    session.IsChanged = false;
                }
                catch (Exception ex)
                {
                    logger.Error("SessionDataError",ex);
                }

            }
        }
    }

    public class Session 
    {
        public bool IsLocal { get; internal set; }
        public bool IsChanged { get; internal set; }
        
        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }

        internal JObject data;
        private SessionManager manager;

        internal Session(SessionManager manager)
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