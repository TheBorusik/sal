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
        protected ILogger logger;
        protected ConnectionMultiplexer redis;

        public SessionManager(ILoggerProvider loggerProvider, IConfigWatcher configWatcher)
        {
            logger = loggerProvider.CreateLogger("SessionManager");
            var redisConfig = configWatcher.GetSection(ConfigurationSectionNames.RedisStore)?.ConvertValue<RedisStoreConfig>();

            if (redisConfig == null)
            {
                redis = null;
                return;
            }

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

            try
            {
                redis = ConnectionMultiplexer.Connect(options);
            }
            catch (Exception e)
            {
                logger.Fatal("нет соединения с редисом", e);
                redis = null;
            }
        }

        [Obsolete]
        public API.Session GetCurrent()
        {
            if (redis == null)
            {
                return new API.Session(this)
                {
                    AuthId = HandlerContext.AuthId,
                    ProcessId = HandlerContext.ProcessId,
                    IsLocal = true,
                    data = new JObject()
                };
            }

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
                        return new API.Session(this)
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
                        logger.Error("SessionDataError", ex);
                    }
                }
            }


            return new API.Session(this)
            {
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                IsLocal = true,
                data = new JObject()
            };
        }


        public void Update(API.Session session)
        {
            if(!session.IsChanged)
                return;
            
            if (redis == null)
                throw SalError.CreateFatalException(SalErrorMessages.RedisNotConfigured);

            var db = redis.GetDatabase();
            
            if (!db.KeyExists(session.SessionId))
                throw SalError.CreateFatalException(SalErrorMessages.SessionNotFound);
            
            var ttl = db.KeyTimeToLive(session.SessionId);
            db.StringSet(session.SessionId, session.data.ToJson(), ttl);
            session.IsChanged = false;
        }

        public void Refresh(API.Session session)
        {
            if (redis == null)
                throw SalError.CreateFatalException(SalErrorMessages.RedisNotConfigured);
            
            var db = redis.GetDatabase();
            
            if (!db.KeyExists(session.SessionId))
                throw SalError.CreateFatalException(SalErrorMessages.SessionNotFound); 
            
            var sessionData = db.StringGet(session.SessionId);
            session.data = JObject.Parse(sessionData);
            session.IsChanged = false;
        }

        public bool TryGetCurrentSession(out API.Session session)
        {
            session = null;
            try
            {
                if (redis == null)
                    return false;

                var sessionId = HandlerContext.SessionId;
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    var db = redis.GetDatabase();
                    if (db.KeyExists(sessionId))
                    {
                        var sessionData = db.StringGet(sessionId);
                        session = new API.Session(this)
                        {
                            SessionId = sessionId,
                            IsChanged = false,
                            data = JObject.Parse(sessionData)
                        };
                        return true;
                    }

                    logger.Warning($"Session Not Found '{sessionId}'");
                    return false;
                }
                logger.Warning($"HandlerContext.SessionId Is Null Or White Space");
            }
            catch (Exception ex)
            {
                logger.Error("Error TryGetCurrent :", ex);
            }

            return false;
        }

        public API.Session GetCurrentSession()
        {
            var sessionId = HandlerContext.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
                throw SalError.CreateFatalException(SalErrorMessages.SessionIdNotSet);

            if (redis == null)
                throw SalError.CreateFatalException(SalErrorMessages.RedisNotConfigured);

            var db = redis.GetDatabase();
            if (!db.KeyExists(sessionId))
                throw SalError.CreateFatalException(SalErrorMessages.SessionNotFound);

            var sessionData = db.StringGet(sessionId);

            return new API.Session(this)
            {
                SessionId = sessionId,
                IsChanged = false,
                data = JObject.Parse(sessionData)
            };
        }
    }
}

