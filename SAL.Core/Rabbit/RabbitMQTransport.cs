using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.API;
using SAL.API.LoggerHelper;
using SAL.API.Monad;
using SAL.Core.Config.Rabbit;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Topology;

namespace SAL.Core.Rabbit
{
    public class RabbitMQTransport : ITransport
    {

        private readonly RabbitMQConnectionManager rabbitMQConnectionManager;


        private readonly ISubscriptionFactory subscriptionFactory;
        private readonly IPublisher publisher;

        public event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;
        public event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;

        public bool IsConnected { get; private set; }
        public bool TopologyInited { get; private set; }
        public ILoggerProvider LoggerProvider { get; private set; } 

        //topology

        private ConcurrentBag<Exchange> exchanges = new ConcurrentBag<Exchange>();
        private ConcurrentBag<Queue> queues = new ConcurrentBag<Queue>();

        private IConfigWatcher configWatcher;
        private readonly ILogger logger;
        private readonly string prefix;

        public RabbitMQTransport(IConfigWatcher configWatcher, ILoggerProvider loggerProvider ,  string prefix)
        {
            this.configWatcher = configWatcher;
            this.prefix = prefix;
            LoggerProvider = loggerProvider;

            var loggerName = $"RMQ.Transport.{prefix}";

            this.logger = loggerProvider.CreateLogger(loggerName);

            var rabbitConfig = configWatcher.GetSection($"{prefix}MessageBus").ToObject<RabbitConfig>();

            if (string.IsNullOrWhiteSpace(rabbitConfig.Host))
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "RMQ Config section not found", properties: new { SectionName = $"{prefix}MessageBus" });
            }


            rabbitMQConnectionManager = new RabbitMQConnectionManager(rabbitConfig, logger);
            rabbitMQConnectionManager.ConnectionFailure += (sender, args) => OnConnectionFailure(args);
            rabbitMQConnectionManager.ConnectionRestore += (sender, args) => OnConnectionRestore(args);


            subscriptionFactory = new RabbitMQAsyncSubscriptionFactory(this);
            publisher = new RabbitMQPublisher(this, LoggerProvider);

            exchanges.Add(new Exchange { Name = ExchangeNames.NotHandledExchange, Type = Topology.ExchangeType.Fanout });
            exchanges.Add(new Exchange { Name = ExchangeNames.CommandExchange, Type = Topology.ExchangeType.Direct, AlternateExchange = ExchangeNames.NotHandledExchange });
            exchanges.Add(new Exchange { Name = ExchangeNames.CommandResultExchange, Type = Topology.ExchangeType.Direct, AlternateExchange = ExchangeNames.NotHandledExchange });
            exchanges.Add(new Exchange { Name = ExchangeNames.EventExchange, Type = Topology.ExchangeType.Direct, AlternateExchange = ExchangeNames.NotHandledExchange });


            queues.Add(new Queue
            {
                Name = QueueNames.NotHandledMessages,
                AutoDelete = false,
                Exclusive = false,
                Durable = true,
                HasDeadLetter = false,
                Expire = null,
                MaxPriority = 0,
                Bindings = new[] { new QueueBinding { ExchangeName = ExchangeNames.NotHandledExchange } },
            });
        }

        internal IModel CreateModel()
        {
            if (IsConnected && TopologyInited)
                return rabbitMQConnectionManager.CreateModel();

            if (!TopologyInited)
                throw new TopologyException();

            throw new NoConnectionException();
        }

        internal void AddQueue(Queue queue)
        {

            if (queues.All(q => !string.Equals(q.Name, queue.Name, StringComparison.InvariantCultureIgnoreCase)))
                queues.Add(queue);

        }

        internal void UpdateTopology()
        {
            if (IsConnected)
                RestoreTopology();
        }

        private void OnConnectionRestore(ConnectionRestoreEventArgs e)
        {

            try
            {

                RestoreTopology();
                IsConnected = true;
                TopologyInited = true;

                var handler = ConnectionRestore;
                handler?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                logger.Error("RestoreTopology : ", ex);
                TopologyInited = false;
            }
        }

        private void OnConnectionFailure(ConnectionFailureEventArgs e)
        {
            IsConnected = false;
            TopologyInited = false;
            var handler = ConnectionFailure;
            handler?.Invoke(this, e);
        }

        private void RestoreTopology()
        {

            exchanges.ForEach(CreateExchange);
            queues.ForEach(CreateQueue);

        }

        private void CreateExchange(Exchange exch)
        {
            using (var channel = rabbitMQConnectionManager.CreateModel())
            {
                var exchangeParams = new Dictionary<string, object>();
                if(!string.IsNullOrWhiteSpace(exch.AlternateExchange))
                    exchangeParams.Add("alternate-exchange", exch.AlternateExchange);
                channel.ExchangeDeclare(exch.Name, exch.Type.ToRMQ(), exch.Durable,false, exchangeParams);
            }
        }

        private void CreateQueue(Queue queue)
        {
            using (var channel = rabbitMQConnectionManager.CreateModel())
            {
                var queueParams = new Dictionary<string, object>();

                if (queue.Expire.HasValue)
                {
                    queueParams.Add("x-expires", (int)queue.Expire.Value.TotalMilliseconds);
                }

                if (queue.HasDeadLetter)
                {
                    queueParams.Add("x-dead-letter-exchange", ExchangeNames.NotHandledExchange);
                }

                if (queue.MaxPriority > 0)
                {
                    queueParams.Add("x-max-priority", queue.MaxPriority);
                }

                var queueData = channel.QueueDeclare(queue.Name, queue.Durable, false, queue.AutoDelete, queueParams);

                queue.Bindings.ForEach(b => { channel?.QueueBind(queueData.QueueName, b.ExchangeName, b.RoutingKey ?? String.Empty); });

                if (queue.Exclusive && queueData.ConsumerCount != 0)
                    throw new CreateExclusiveQueueException();
            }

        }

        public void Start()
        {
            rabbitMQConnectionManager.Start();
        }

        public void Stop()
        {
            rabbitMQConnectionManager.Stop();
        }

        public ISubscriptionFactory CreateMessageSubscription()
        {
            return subscriptionFactory;
        }

        public IPublisher CreatePublisher()
        {
            return publisher;
        }

        public bool IsQueuePresent(string queueName)
        {
            return queues.Any(q => string.Equals(q.Name, queueName, StringComparison.InvariantCultureIgnoreCase));
        }


        public void Dispose()
        {
            Stop();
        }
    }
}