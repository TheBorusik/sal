using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using SAL.Core.MessageBus.Abstractions;

namespace SAL.Core.MessageBus.Nats
{
    /// <summary>
    /// Реализация шины сообщений на базе NATS JetStream
    /// </summary>
    public class NatsJetStreamMessageBus : IMessageBus
    {
        private readonly IConnection _connection;
        private readonly IJetStream _jetStream;
        private readonly ILogger<NatsJetStreamMessageBus> _logger;
        private readonly string _streamName;
        private bool _disposed;

        public event EventHandler ConnectionRestored;
        public event EventHandler ConnectionFailed;

        public bool IsConnected => _connection is { IsClosed: false };
        public string TransportName => "NATS JetStream";

        public NatsJetStreamMessageBus(
            IConnection connection,
            IJetStream jetStream,
            string streamName,
            ILogger<NatsJetStreamMessageBus> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
            _streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            _logger.LogInformation("Starting NATS JetStream Message Bus");
            // JetStream не требует явного запуска, соединение устанавливается при создании
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping NATS JetStream Message Bus");
            // Очистка подписчиков выполняется при Dispose
        }

        public IMessagePublisher CreatePublisher()
        {
            return new NatsJetStreamPublisher(_jetStream, _streamName);
        }

        public IMessageSubscriberFactory CreateSubscriberFactory()
        {
            return new NatsJetStreamSubscriberFactory(_jetStream, _streamName);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Издатель сообщений NATS JetStream
    /// </summary>
    public class NatsJetStreamPublisher : IMessagePublisher
    {
        private readonly IJetStream _jetStream;
        private readonly string _streamName;
        private bool _disposed;

        public NatsJetStreamPublisher(IJetStream jetStream, string streamName)
        {
            _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
            _streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
        }

        public async Task PublishAsync(string subject, Message message)
        {
            var natsMsg = new Msg
            {
                Subject = $"{_streamName}.{subject}",
                Data = message.Payload ?? Array.Empty<byte>(),
                Headers = new Headers()
            };

            // Добавляем метаданные в заголовки
            natsMsg.Headers.Add("X-Message-Id", message.Id);
            if (!string.IsNullOrEmpty(message.CorrelationId))
                natsMsg.Headers.Add("X-Correlation-Id", message.CorrelationId);
            if (!string.IsNullOrEmpty(message.ReplyTo))
                natsMsg.Headers.Add("X-Reply-To", message.ReplyTo);
            natsMsg.Headers.Add("X-Timestamp", message.Timestamp.ToString("O"));
            if (!string.IsNullOrEmpty(message.ContentType))
                natsMsg.Headers.Add("Content-Type", message.ContentType);

            await _jetStream.PublishAsync(natsMsg);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Фабрика подписчиков NATS JetStream
    /// </summary>
    public class NatsJetStreamSubscriberFactory : IMessageSubscriberFactory
    {
        private readonly IJetStream _jetStream;
        private readonly string _streamName;

        public NatsJetStreamSubscriberFactory(IJetStream jetStream, string streamName)
        {
            _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
            _streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
        }

        public IMessageSubscriber CreateSubscriber(string consumerGroup, params string[] subjects)
        {
            return new NatsJetStreamSubscriber(_jetStream, _streamName, consumerGroup, subjects);
        }
    }

    /// <summary>
    /// Подписчик на сообщения NATS JetStream
    /// </summary>
    public class NatsJetStreamSubscriber : IMessageSubscriber
    {
        private readonly IJetStream _jetStream;
        private readonly string _streamName;
        private readonly string _consumerGroup;
        private readonly string[] _subjects;
        private IJetStreamPushAsyncSubscription _subscription;
        private bool _disposed;

        public NatsJetStreamSubscriber(
            IJetStream jetStream,
            string streamName,
            string consumerGroup,
            string[] subjects)
        {
            _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
            _streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            _consumerGroup = consumerGroup ?? throw new ArgumentNullException(nameof(consumerGroup));
            _subjects = subjects ?? throw new ArgumentNullException(nameof(subjects));
        }

        public void Start()
        {
            // Создание или получение consumer
            var consumerOptions = new ConsumerConfiguration
            {
                DurableName = _consumerGroup,
                AckPolicy = AckPolicy.Explicit,
                DeliverPolicy = DeliverPolicy.All
            };

            // Подписка на.subjects
            _subscription = _jetStream.SubscribeAsync(
                $"{_streamName}.>",
                consumerOptions,
                OnMessageReceived);
        }

        private async Task OnMessageReceived(MsgHandlerEventArgs args)
        {
            try
            {
                var message = ParseMessage(args.Message);
                // Здесь должен быть вызов обработчика сообщений
                // Для демонстрации просто подтверждаем сообщение
                await args.Message.AckAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error processing message: {ex.Message}");
                await args.Message.NakAsync();
            }
        }

        private Message ParseMessage(NATS.Client.Msg msg)
        {
            var message = new Message
            {
                Payload = msg.Data,
                ContentType = msg.Headers?.GetValue("Content-Type") ?? "application/json"
            };

            if (msg.Headers != null)
            {
                message.Id = msg.Headers.GetValue("X-Message-Id") ?? Guid.NewGuid().ToString("N");
                message.CorrelationId = msg.Headers.GetValue("X-Correlation-Id");
                message.ReplyTo = msg.Headers.GetValue("X-Reply-To");
                
                if (DateTime.TryParse(msg.Headers.GetValue("X-Timestamp"), out var timestamp))
                {
                    message.Timestamp = timestamp;
                }
            }

            return message;
        }

        public void Stop()
        {
            _subscription?.Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
