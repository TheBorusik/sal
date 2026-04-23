namespace SAL.Core.Transport
{
    using System;
    using SAL.Core.Rabbit.Interfaces;

    /// <summary>
    /// Базовый интерфейс транспортного уровня для обмена сообщениями
    /// Абстракция от конкретного брокера сообщений (RabbitMQ, NATS JetStream, etc.)
    /// </summary>
    public interface IMessageTransport : IDisposable
    {
        /// <summary>
        /// Запуск транспорта
        /// </summary>
        void Start();

        /// <summary>
        /// Остановка транспорта
        /// </summary>
        void Stop();

        /// <summary>
        /// Статус подключения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Имя контура (Front/Back)
        /// </summary>
        string ContourName { get; }

        /// <summary>
        /// Событие восстановления соединения
        /// </summary>
        event EventHandler ConnectionRestore;

        /// <summary>
        /// Событие потери соединения
        /// </summary>
        event EventHandler ConnectionFailure;

        /// <summary>
        /// Создание фабрики подписок
        /// </summary>
        ISubscriptionFactory CreateSubscriptionFactory();

        /// <summary>
        /// Создание издателя
        /// </summary>
        IPublisher CreatePublisher();
    }
}
