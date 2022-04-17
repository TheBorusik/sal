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

        public ISubscription CreateSystemEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SystemEvent")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_SystemEvent";

            var bindings = eventNames.Select(s => new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"!{s}"
            }).ToList();

            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"!{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}"
            });
            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"!{AdapterConfiguration.AdapterType}@"
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
                Bindings = bindings.ToArray()
            });

            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = prefetchCount
            });

            return new MultiConsumerSubscriptionAsync(transport, subscriptionName, prefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Event")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}_Event";

            var bindings = eventNames.Select(s => new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = s
            }).ToList();

            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}"
            });
            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"{AdapterConfiguration.AdapterType}@"
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
                Bindings = bindings.ToArray()
            });

            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = prefetchCount
            });
            
            queueName = $"#{AdapterConfiguration.AdapterType}_Event";
            bindings = new List<QueueBinding>();
            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.CEventExchange,
                RoutingKey = $"{AdapterConfiguration.AdapterType}@"
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
                Bindings = bindings.ToArray()
            });
            
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = prefetchCount
            });

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
                    new QueueBinding
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
                    new QueueBinding
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
                    new QueueBinding
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
                        new QueueBinding
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
                    new QueueBinding
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
                    new QueueBinding
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
                        new QueueBinding
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