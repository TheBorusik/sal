using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.API;
using SAL.Core.Configuration.Rabbit;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Topology;
using SAL.Infrastructure;

namespace SAL.Core.Rabbit
{
    public class RabbitMQTransportAsync :  IRMQTransport
    {
        private readonly RabbitMQConnectionManagerAsync rabbitMQConnectionManager;


        private readonly ISubscriptionFactory subscriptionFactory;
        private readonly RabbitMQPublisherWithConfirms publisher;

        public event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;
        public event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;

        public bool IsConnected => rabbitMQConnectionManager.IsConnected;
        public string CounterName => rabbitMQConnectionManager.ContourName;
        public ILoggerProvider LoggerProvider { get; private set; }

        //topology

        private LinkedList<Exchange> exchanges = new();
        private object exchangeLocker = new();
        private ConcurrentBag<Queue> queues = new();

        private readonly ILogger logger;
        private readonly Contour contour;
        private readonly IHostApplicationLifetime lifeTime;
        
        public RabbitMQTransportAsync(IHostApplicationLifetime lifeTime, IConfigWatcher configWatcher, ILoggerProvider loggerProvider, Contour contour)
        {

            this.contour = contour;
            this.lifeTime = lifeTime;
            LoggerProvider = loggerProvider;

            var loggerName = $"RMQ.Transport.{contour}";

            this.logger = loggerProvider.CreateLogger(loggerName);
            
            var sectionName = contour == Contour.Back ? "MessageBus" : $"{contour}MessageBus";

            var rabbitConfig = configWatcher.GetSection(sectionName)?.ToObject<RabbitConfig>();
            

            if (string.IsNullOrWhiteSpace(rabbitConfig?.Host))
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "RMQ Config section not found", properties: new { SectionName = $"{contour}MessageBus" });
            }


            rabbitMQConnectionManager = new RabbitMQConnectionManagerAsync(rabbitConfig, loggerProvider);
            rabbitMQConnectionManager.ConnectionFailure += (sender, args) => OnConnectionFailure(args);
            
            subscriptionFactory = new RabbitMQAsyncSubscriptionFactoryAsync(this);
            publisher = new RabbitMQPublisherWithConfirms(this, LoggerProvider);

            exchanges.AddLast(new Exchange { Name = ExchangeNames.NotHandledExchange, Type = Topology.ExchangeType.Fanout });
            exchanges.AddLast(new Exchange { Name = ExchangeNames.EventExchange, Type = Topology.ExchangeType.Direct });
            exchanges.AddLast(new Exchange { Name = ExchangeNames.CommandExchange, Type = Topology.ExchangeType.Direct, AlternateExchange = ExchangeNames.NotHandledExchange });
            exchanges.AddLast(new Exchange { Name = ExchangeNames.CommandResultExchange, Type = Topology.ExchangeType.Direct, AlternateExchange = ExchangeNames.NotHandledExchange });


            
            queues.Add(new Queue
            {
                Name = QueueNames.NotHandledMessages,
                AutoDelete = false,
                Exclusive = false,
                Durable = true,
                HasDeadLetter = false,
                Expire = null,
                MaxPriority = 0,
                Bindings = new[] { new Binding { ExchangeName = ExchangeNames.NotHandledExchange } },
            });
        }

        public ILogger CreateLogger(string name)
        {
            return LoggerProvider.CreateLogger(name);
        }

        public IModel CreateModel()
        {
            if (IsConnected)
                return rabbitMQConnectionManager.CreateModel();
            
            throw new NoConnectionException();
        }

        public void AddQueue(Queue queue)
        {

            if (queues.All(q => !string.Equals(q.Name, queue.Name, StringComparison.InvariantCultureIgnoreCase)))
                queues.Add(queue);

        }

        public void AddExchange(Exchange exchange)
        {
            lock (exchangeLocker)
            {
                if (exchanges.All(e => !string.Equals(e.Name, exchange.Name, StringComparison.InvariantCultureIgnoreCase)))
                    exchanges.AddLast(exchange);
            }
        }

        private void OnConnectionFailure(ConnectionFailureEventArgs e)
        {
            lifeTime.StopApplication();
            //    salService.Stop();
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
                if (!string.IsNullOrWhiteSpace(exch.AlternateExchange))
                    exchangeParams.Add("alternate-exchange", exch.AlternateExchange);
                channel.ExchangeDeclare(exch.Name, exch.Type.ToRMQ(), exch.Durable, false, exchangeParams);
                exch.Bindings.ForEach(b => { channel?.ExchangeBind(exch.Name, b.ExchangeName, b.RoutingKey ?? String.Empty); });
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
                    if(!string.IsNullOrWhiteSpace(queue.DeadLetterExchange))
                        queueParams.Add("x-dead-letter-exchange", queue.DeadLetterExchange);
                    else
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
            RestoreTopology();
            publisher.Start();
        }

        public void Stop()
        {
            publisher.Stop();
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