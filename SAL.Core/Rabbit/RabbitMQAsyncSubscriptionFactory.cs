using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAL.API;
using SAL.API.Monad;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Subscription;
using SAL.Core.Rabbit.Topology;

namespace SAL.Core.Rabbit
{
    public class RabbitMQAsyncSubscriptionFactory : ISubscriptionFactory
    {
        private readonly RabbitMQTransport transport;

        public RabbitMQAsyncSubscriptionFactory(RabbitMQTransport transport)
        {
            this.transport = transport;
        }

        public ISubscription CreateSystemEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SystemEvent")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}" + "#SystemEvent";

            var bindings = eventNames.Select(s => new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = s
            }).ToList();

            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"System#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}"
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

            transport.UpdateTopology();

            return new MultiConsumerSubscription(transport, subscriptionName, prefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Event")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}" + "#Event";

            var bindings = eventNames.Select(s => new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = s
            }).ToList();

            bindings.Add(new QueueBinding
            {
                ExchangeName = ExchangeNames.EventExchange,
                RoutingKey = $"{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}"
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

            transport.UpdateTopology();

            return new MultiConsumerSubscription(transport, subscriptionName, prefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateCommandResult(ushort globalPrefetchCount, ushort instancePrefetchCount, ushort typePrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "CommandResults")
        {

            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}" + "#CommandResults";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]{ new QueueBinding
                {
                    ExchangeName = ExchangeNames.CommandResultExchange,
                    RoutingKey = $"{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}",
                }}
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = instancePrefetchCount
            });


            queueName = $"#{AdapterConfiguration.AdapterType}" + "#CommandResults";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = false,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]{ new QueueBinding
                {
                    ExchangeName = ExchangeNames.CommandResultExchange,
                    RoutingKey = $"{AdapterConfiguration.AdapterType}",
                }}
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = typePrefetchCount
            });

            transport.UpdateTopology();

            return new MultiConsumerSubscription(transport, subscriptionName, globalPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateSyncCommandResult(ushort syncPrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SyncCommandResults")
        {
            var queueList = new List<QueueInfo>();

            var queueName = $"#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}" + "#Sync#CommandResults";
            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]{ new QueueBinding
                {
                    ExchangeName = ExchangeNames.CommandResultExchange,
                    RoutingKey = $"{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}#Sync",
                }}
            });
            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = syncPrefetchCount
            });

            transport.UpdateTopology();

            return new MultiConsumerSubscription(transport, subscriptionName, syncPrefetchCount, queueList.ToArray(), handler);
        }

        public ISubscription CreateCommand(ushort globalPrefetchCount, ushort mainPrefetchCount, CommandInfo[] commands, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Command")
        {
            var queueList = new List<QueueInfo>();

            string queueName;
            commands.ForEach(c =>
            {
                queueName = "Command." + c.CommandName;
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
                    Bindings = new[]{ new QueueBinding
                    {
                        ExchangeName = ExchangeNames.CommandExchange,
                        RoutingKey = c.CommandName,
                    }}
                });
            });

            queueName = $"#{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}" + "#Commands";

            transport.AddQueue(new Queue
            {
                Name = queueName,
                AutoDelete = false,
                MaxPriority = 9,
                Exclusive = true,
                HasDeadLetter = true,
                Expire = null,
                Durable = true,
                Bindings = new[]{ new QueueBinding
                {
                    ExchangeName = ExchangeNames.CommandExchange,
                    RoutingKey = $"{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}",
                }}
            });

            queueList.Add(new QueueInfo
            {
                QueueName = queueName,
                PrefetchCount = mainPrefetchCount
            });

            transport.UpdateTopology();

            return new MultiConsumerSubscription(transport, subscriptionName, globalPrefetchCount, queueList.ToArray(), handler);
        }

        public void Dispose()
        {

        }
    }

    public class CommandInfo
    {
        public string CommandName { get; set; }
        public ushort PrefetchCount { get; set; }
    }
}