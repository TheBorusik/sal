using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.MessageBus.Abstractions;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.MessageBus.RabbitMQ
{
    /// <summary>
    /// Реализация шины сообщений на базе RabbitMQ
    /// </summary>
    public class RabbitMQMessageBus : IMessageBus
    {
        private readonly ITransport _transport;
        private readonly ILogger<RabbitMQMessageBus> _logger;
        private bool _disposed;

        public event EventHandler ConnectionRestored;
        public event EventHandler ConnectionFailed;

        public bool IsConnected => _transport.IsConnected;
        public string TransportName => "RabbitMQ";

        public RabbitMQMessageBus(ITransport transport, ILogger<RabbitMQMessageBus> logger)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Подписка на события транспорта
            _transport.ConnectionRestore += (s, e) => ConnectionRestored?.Invoke(this, EventArgs.Empty);
            _transport.ConnectionFailure += (s, e) => ConnectionFailed?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            _logger.LogInformation("Starting RabbitMQ Message Bus");
            _transport.Start();
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping RabbitMQ Message Bus");
            _transport.Stop();
        }

        public IMessagePublisher CreatePublisher()
        {
            return new RabbitMQMessagePublisher(_transport.CreatePublisher());
        }

        public IMessageSubscriberFactory CreateSubscriberFactory()
        {
            return new RabbitMQSubscriberFactory(_transport.CreateMessageSubscription());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transport?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Издатель сообщений RabbitMQ
    /// </summary>
    public class RabbitMQMessagePublisher : IMessagePublisher
    {
        private readonly IPublisher _innerPublisher;
        private bool _disposed;

        public RabbitMQMessagePublisher(IPublisher innerPublisher)
        {
            _innerPublisher = innerPublisher ?? throw new ArgumentNullException(nameof(innerPublisher));
        }

        public async Task PublishAsync(string exchange, Message message)
        {
            var rabbitMessage = new RabbitMessage
            {
                Exchange = exchange,
                RoutingKey = message.Id,
                Payload = message.Payload,
                ContentType = message.ContentType,
                CorrelationId = message.CorrelationId,
                ReplyTo = message.ReplyTo,
                Timestamp = message.Timestamp
            };

            await _innerPublisher.PublishAsync(rabbitMessage);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                (_innerPublisher as IDisposable)?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Фабрика подписчиков RabbitMQ
    /// </summary>
    public class RabbitMQSubscriberFactory : IMessageSubscriberFactory
    {
        private readonly ISubscriptionFactory _subscriptionFactory;

        public RabbitMQSubscriberFactory(ISubscriptionFactory subscriptionFactory)
        {
            _subscriptionFactory = subscriptionFactory ?? throw new ArgumentNullException(nameof(subscriptionFactory));
        }

        public IMessageSubscriber CreateSubscriber(string queueName, params string[] routingKeys)
        {
            // Здесь должна быть логика создания подписки на очередь с указанными routing keys
            // Для упрощения используем существующую фабрику
            var subscription = _subscriptionFactory.CreateSubscription(queueName);
            return new RabbitMQMessageSubscriber(subscription);
        }
    }

    /// <summary>
    /// Подписчик на сообщения RabbitMQ
    /// </summary>
    public class RabbitMQMessageSubscriber : IMessageSubscriber
    {
        private readonly ISubscription _subscription;
        private bool _disposed;

        public RabbitMQMessageSubscriber(ISubscription subscription)
        {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        }

        public void Start()
        {
            _subscription.Start();
        }

        public void Stop()
        {
            _subscription.Stop();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _subscription?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
