using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Configuration.Messages;
using SAL.Core.Exceptions;
using SAL.Core.Helpers;
using StackExchange.Redis;
using MessageTypes = SAL.Core.Configuration.Messages.MessageTypes;

namespace SAL.Core.Configuration
{
    public class RemoteConfigWatcher : IConfigWatcher
        //,IEventHandler<>
    {
        private ConnectionMultiplexer redis;
        private ISubscriber subscriber;

        private readonly ConcurrentDictionary<string, ConfigExecuteHandlerInfo> resultHandlers = new();
        private readonly Service baseServiceSection = new Service();


        private JObject configurationRoot;
        private string configurationId;
        private string configurationName;
        private readonly JsonMergeSettings mergeSettings;
        private bool inited = false;
        private const string configurationBusName = "configurationBus";

        private readonly EventHandlerList<string, ConfigurationSectionChangedEventArgs> listEventDelegates = new();

        public RemoteConfigWatcher()
        {
            mergeSettings = new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            };
        }

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


        public void Init()
        {
            if (inited)
                return;
           
            var confBusStr = Environment.GetEnvironmentVariable("ConfBus") ?? "configurationBus";
            
            var confBusSplit = confBusStr.Split("#");
            var busHost = confBusSplit.First();

            if (!int.TryParse(confBusSplit.Last(), out var busPort))
            {
                busPort = 6379;
            }
            
            Console.WriteLine($"Connect to configurationBus (host:{busHost} port:{busPort} )...");
            
            var options = new ConfigurationOptions
            {
                EndPoints = 
                {
                    { busHost, busPort}
                },
                Ssl = false,
                DefaultDatabase = 0
            };
            ///
            try
            {
                redis = ConnectionMultiplexer.Connect(options);
            }
            catch (RedisConnectionException e)
            {
                throw new ConfigurationErrorException(e.Message);
            }

            subscriber = redis.GetSubscriber();

            subscriber.Subscribe(configurationBusName).OnMessage(RedisHandler);

            Console.WriteLine("Connected to configurationBus");
            Console.WriteLine($"Geting AdapterName for type {AdapterConfiguration.AdapterType}");
            var result = GetAdapterName(e => throw new ConfigurationErrorException(e.Code)).Result;
            baseServiceSection.AdapterName = result.AdapterName;
            baseServiceSection.LogRoot = "";
            baseServiceSection.RootStorePath = "";


            Console.WriteLine($"Geting Adapter configuration for {AdapterConfiguration.AdapterType}.{baseServiceSection.AdapterName}");
            configurationRoot = GetConfigFromBus();
            configurationId = result.ConfigurationId;
            configurationName = result.ConfigurationName;
            Console.WriteLine($"Config Received Name:'{result.ConfigurationName}' | Id:{result.ConfigurationId}");
        }

        private JObject GetConfigFromBus()
        {
            var key = $"{AdapterConfiguration.AdapterType}.{baseServiceSection.AdapterName}";
            var db = redis.GetDatabase();
            var value = db.StringGet(key);
            if (!value.HasValue) throw new ConfigurationErrorException("Config not found");
            db.KeyDelete(key);
            var config = JToken.Parse(value).RemoveEmptyChildren() as JObject;
            if (config == null) 
                throw new ConfigurationErrorException("Config is null");
            
            config.AddOrUpdate(ConfigurationSectionNames.Service, baseServiceSection);
            return config.Clone();
        }

        private void RedisHandler(ChannelMessage msg)
        {
            var configMessage = msg.Message.ToString().ConvertValue<ConfigMessage>();
            if(configMessage.Source == AdapterConfiguration.AdapterFullName)
                return;
            
            var dt = DateTime.UtcNow - configMessage.Timestamp;
            
            if (resultHandlers.TryRemove(configMessage.CorrelationId, out var handler))
            {
                handler.CancellationTokenSource.Dispose();
                handler.CompletionSource.TrySetResult(configMessage);
                return;
            }

            if (configMessage.Type == MessageTypes.ConfigChanged)
            {
                var configChanged = configMessage.Payload.ConvertValue<ConfigChanged>();
                if(configChanged.ChangedConfigurationIds.Contains(configurationId))
                    OnConfigChanged();
            }
        }

        private void RegisterExecHandler(string correlationId, TimeSpan timeOut, TaskCompletionSource<ConfigMessage> completionSource)
        {
            var info = new ConfigExecuteHandlerInfo
            {
                CompletionSource = completionSource,
                CancellationTokenSource = new CancellationTokenSource()
            };

            resultHandlers.TryAdd(correlationId, info);

            info.CancellationTokenSource.Token.Register(() =>
            {
                if (resultHandlers.TryRemove(correlationId, out var scrh))
                {
                    scrh.CompletionSource.TrySetResult(new ConfigMessage
                    {
                        CorrelationId = correlationId,
                        Type = MessageTypes.Error,
                        Payload = JObject.FromObject(new Error
                        {
                            Code = ConfigErrorCodes.Timeout
                        }),
                        Timestamp = DateTime.UtcNow,
                        Source = AdapterConfiguration.AdapterFullName
                    });

                    scrh.CancellationTokenSource.Dispose();
                }
            });
            info.CancellationTokenSource.CancelAfter(timeOut);
        }

        private async Task<GetAdapterNameRes> GetAdapterName(Func<Error, Task<GetAdapterNameRes>> onError)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            
            var taskSlot =  Environment.GetEnvironmentVariable("X_TASK_SLOT");
            var returnDefaultStr =  Environment.GetEnvironmentVariable("UseDefaultConfiguration");
            var returnDefaultConfiguration = true;
            if (bool.TryParse(returnDefaultStr, out var value))
                returnDefaultConfiguration = value;
            if (int.TryParse(returnDefaultStr, out var intVal))
            {
                if (intVal == 0)
                    returnDefaultConfiguration = false;
            }
            
            var configurationName = Environment.GetEnvironmentVariable("ConfigurationName");
                
                

            var message = new ConfigMessage
            {
                Source = AdapterConfiguration.AdapterFullName,
                Timestamp = DateTime.UtcNow,
                Type = MessageTypes.GetAdapterName,
                CorrelationId = correlationId,
                Payload = JObject.FromObject(new GetAdapterNameReq
                {
                    AdapterType = AdapterConfiguration.AdapterType,
                    MachineName = AdapterConfiguration.MachineName,
                    TaskSlot = taskSlot,
                    ReturnDefaultConfiguration = returnDefaultConfiguration,
                    ConfigurationName = configurationName,
                    InDocker = AdapterConfiguration.InDocker
                })
            };

            TaskCompletionSource<ConfigMessage> completionSource = new();

            RegisterExecHandler(correlationId, TimeSpan.FromSeconds(20), completionSource);

            await subscriber.PublishAsync(configurationBusName, message.ToJson());

            var result = await completionSource.Task;

            if (result.Type == MessageTypes.Error)
            {
                return await onError(result.Payload.ToObject<Error>());
            }

            return result.Payload.ToObject<GetAdapterNameRes>();
        }


        private void OnConfigChanged()
        {
            JObject tmpConfig;
            try
            {
                tmpConfig = GetConfigFromBus();
            }
            catch (Exception)
            {
                return;
            }
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
        }


        public void Done()
        {
            subscriber?.UnsubscribeAll();
        }


        public void UpdateSection(string sectionName, JObject newData)
        {
            
        }
        
        public void MergeSection(string sectionName, JObject mergeData)
        {
        }

        public bool CanUpdateConfig()
        {
            return false;
        }
    }
}