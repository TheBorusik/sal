using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAL.API;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Subscription;
using SAL.Core.Rabbit.Topology;

namespace SAL.Core.Rabbit
{
    internal class RabbitMQAsyncSubscriptionFactoryAsync : ISubscriptionFactory
    {
        private readonly IRMQTransport transport;

        public RabbitMQAsyncSubscriptionFactoryAsync(IRMQTransport transport)
        {
            this.transport = transport;
        }
        
        public ISubscription CreateEvent(ushort prefetchCount, IEnumerable<EventInfo> eventInfosEnumerable, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Event")
        {

            var eventInfos = eventInfosEnumerable.ToArray(); 
            
            //exchanges
            var typeEx = new Exchange
            {
                Durable = true,
                Type = ExchangeType.Direct,
                Name = $"{AdapterConfiguration.AdapterType}:EventExchange",
                Bindings = eventInfos
                    .Where(x => x.OneInstance)
                    .Select(x => new Binding{ ExchangeName = ExchangeNames.EventExchange, RoutingKey = x.EventName})
                    .ToArray()
            };

            transport.AddExchange(typeEx);
            
            var instEx = new Exchange
            {
                Durable = true,
                Type = ExchangeType.Direct,
                Name = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}:EventExchange",
                Bindings = eventInfos
                    .Where(x => !x.OneInstance)
                    .Select(x => new Binding{ ExchangeName = ExchangeNames.EventExchange, RoutingKey = x.EventName})
                    .ToArray()
            };
            transport.AddExchange(instEx);

            
            //queue
            
            var queueList = new List<QueueInfo>();
            var queueName = "";


            if (eventInfos.Any(x => x.OneInstance && !x.Preserved))
            {
                queueName = $"#{AdapterConfiguration.AdapterType}_OnlineEvent";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = 0
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = true,
                    MaxPriority = 0,
                    Exclusive = false,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = eventInfos
                        .Where(x => x.OneInstance && !x.Preserved)
                        .Select(x => new Binding { ExchangeName = typeEx.Name, RoutingKey = x.EventName })
                        .ToArray()
                });
            }

            if (eventInfos.Any(x => x.OneInstance && x.Preserved))
            {
                queueName = $"#{AdapterConfiguration.AdapterType}_Event";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = 0
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = false,
                    MaxPriority = 0,
                    Exclusive = false,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = eventInfos
                        .Where(x => x.OneInstance && x.Preserved)
                        .Select(x => new Binding { ExchangeName = typeEx.Name, RoutingKey = x.EventName })
                        .ToArray()
                });
            }
            
            if (eventInfos.Any(x => !x.OneInstance && !x.Preserved))
            {
                queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_OnlineEvent";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = 0
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = true,
                    MaxPriority = 0,
                    Exclusive = true,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = eventInfos
                        .Where(x => !x.OneInstance && !x.Preserved)
                        .Select(x => new Binding { ExchangeName = instEx.Name, RoutingKey = x.EventName })
                        .ToArray()
                });
            }
            
            if (eventInfos.Any(x => !x.OneInstance && x.Preserved))
            {



                queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_Event";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = 0
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = false,
                    MaxPriority = 0,
                    Exclusive = true,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = eventInfos
                        .Where(x => !x.OneInstance && x.Preserved)
                        .Select(x => new Binding { ExchangeName = instEx.Name, RoutingKey = x.EventName })
                        .ToArray()
                });
            }

            return new MultiConsumerSubscriptionAsync(transport, subscriptionName, prefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateCommandResult(ushort globalPrefetchCount, ushort instancePrefetchCount, ushort typePrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "CommandResults")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_CommandResults";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]
                {
                    new Binding
                    {
                        ExchangeName = ExchangeNames.CommandResultExchange,
                        RoutingKey = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}",
                    }
                }
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = instancePrefetchCount
            });


            queueName = $"#{AdapterConfiguration.AdapterType}_CommandResults";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = false,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]
                {
                    new Binding
                    {
                        ExchangeName = ExchangeNames.CommandResultExchange,
                        RoutingKey = $"{AdapterConfiguration.AdapterType}@",
                    }
                }
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = typePrefetchCount
            });

            return new MultiConsumerSubscriptionAsync(transport, subscriptionName, globalPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateSyncCommandResult(ushort syncPrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SyncCommandResults")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_CommandResultsSync";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]
                {
                    new Binding
                    {
                        ExchangeName = ExchangeNames.CommandResultExchange,
                        RoutingKey = $"!{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}",
                    }
                }
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = syncPrefetchCount
            });

            return new MultiConsumerSubscriptionAsync(transport, subscriptionName, syncPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateCommand(ushort globalPrefetchCount, ushort mainPrefetchCount, CommandInfo[] commands, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Command")
        {
            var queueList = new List<QueueInfo>();

            string queueName;
            commands.ForEach(c =>
            {
                queueName = $"${c.CommandName}";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = c.PrefetchCount
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = false,
                    MaxPriority = 9,
                    Exclusive = false,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = new[]
                    {
                        new Binding
                        {
                            ExchangeName = ExchangeNames.CommandExchange,
                            RoutingKey = c.CommandName,
                        }
                    }
                });
            });

            queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_Commands";

            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]
                {
                    new Binding
                    {
                        ExchangeName = ExchangeNames.CommandExchange,
                        RoutingKey = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}",
                    }
                }
            });

            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = mainPrefetchCount
            });


            queueName = $"#{AdapterConfiguration.AdapterType}_Commands";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = false,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]
                {
                    new Binding
                    {
                        ExchangeName = ExchangeNames.CommandExchange,
                        RoutingKey = $"{AdapterConfiguration.AdapterType}@",
                    }
                }
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = mainPrefetchCount
            });


            return new MultiConsumerSubscriptionAsync(transport, subscriptionName, globalPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateExternalHttp(ushort globalPrefetchCount, ExternalHttpInfo[] queues, Func<RabbitMessageEx, Action, Action, Task> handler, string subscriptionName = "ExternalHttp")
        {
            var queueList = new List<QueueInfo>();
            string queueName;
            queues.ForEach(q =>
            {
                queueName = $"%{q.Path.ToLower()}";
                queueList.Add(new QueueInfo
                {
                    QueueName = queueName,
                    PrefetchCount = q.PrefetchCount
                });
                transport.AddQueue(new Queue
                {
                    Name = queueName,
                    AutoDelete = false,
                    MaxPriority = 0,
                    Exclusive = false,
                    HasDeadLetter = true,
                    Expire = null,
                    Durable = true,
                    Bindings = new[]
                    {
                        new Binding
                        {
                            ExchangeName = ExchangeNames.CommandExchange,
                            RoutingKey = q.Path,
                        }
                    }
                });
            });
              
            return new MultiConsumerSubscriptionAsyncEx(transport, subscriptionName, globalPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateCustom(ushort globalPrefetchCount, QueueInfo[] queues, Func<RabbitMessageEx, Action, Action, Task> handler, string subscriptionName = "Custom")
        {
            return new MultiConsumerSubscriptionAsyncEx(transport, subscriptionName, globalPrefetchCount, queues, handler);
        }
        
        public void Dispose()
        {
        }
    }
}