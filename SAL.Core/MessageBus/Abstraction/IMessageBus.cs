using System;
using System.Threading.Tasks;

namespace SAL.Core.MessageBus.Abstractions
{
    /// <summary>
    /// Базовое сообщение для транспортной шины
    /// </summary>
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public byte[] Payload { get; set; }
        public string ContentType { get; set; } = "application/json";
    }

    /// <summary>
    /// Интерфейс издателя сообщений
    /// </summary>
    public interface IMessagePublisher : IDisposable
    {
        Task PublishAsync(string exchangeOrSubject, Message message);
    }

    /// <summary>
    /// Интерфейс подписчика на сообщения
    /// </summary>
    public interface IMessageSubscriber : IDisposable
    {
        void Start();
        void Stop();
    }

    /// <summary>
    /// Обработчик входящих сообщений
    /// </summary>
    public interface IMessageHandler
    {
        Task HandleAsync(Message message);
    }

    /// <summary>
    /// Фабрика подписчиков
    /// </summary>
    public interface IMessageSubscriberFactory
    {
        IMessageSubscriber CreateSubscriber(string queueOrGroup, params string[] subjects);
    }

    /// <summary>
    /// Основной интерфейс транспортной шины сообщений
    /// </summary>
    public interface IMessageBus : IDisposable
    {
        void Start();
        void Stop();

        event EventHandler ConnectionRestored;
        event EventHandler ConnectionFailed;

        bool IsConnected { get; }
        string TransportName { get; }

        IMessagePublisher CreatePublisher();
        IMessageSubscriberFactory CreateSubscriberFactory();
    }
}
