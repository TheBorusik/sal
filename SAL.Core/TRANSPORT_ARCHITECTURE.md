# Архитектура транспорта сообщений SAL

## Обзор

Данный документ описывает архитектуру транспортного уровня SAL (System Adapter Layer), которая поддерживает работу с различными брокерами сообщений через единый интерфейс.

## Интерфейс IMessageTransport

Базовый интерфейс `IMessageTransport` определен в файле `SAL.Core/Transport/IMessageTransport.cs` и предоставляет абстракцию для работы с брокерами сообщений:

```csharp
public interface IMessageTransport : IDisposable
{
    void Start();
    void Stop();
    bool IsConnected { get; }
    string ContourName { get; }
    
    event EventHandler ConnectionRestore;
    event EventHandler ConnectionFailure;
    
    ISubscriptionFactory CreateSubscriptionFactory();
    IPublisher CreatePublisher();
}
```

## Поддерживаемые транспорты

### 1. RabbitMQ (существующая реализация)

**Класс:** `RabbitMQTransportAsync`  
**Расположение:** `SAL.Core/Rabbit/RabbitMQTransportAsync.cs`

Реализует интерфейс `IRMQTransport` (расширение `IMessageTransport`) и предоставляет полную совместимость с существующим кодом.

**Особенности:**
- Полная поддержка всех типов подписок (Event, Command, CommandResult, ExternalHttp, Custom)
- Топология очередей и exchange'ей
- Publisher confirms
- Восстановление соединения

### 2. NATS JetStream (новая реализация)

**Класс:** `NatsJetStreamTransport`  
**Расположение:** `SAL.Core/Nats/NatsJetStreamTransport.cs`

Реализует интерфейс `IMessageTransport` для работы с NATS JetStream.

**Компоненты:**
- `NatsPublisher` - публикация сообщений
- `NatsSubscription` - подписка на сообщения
- `NatsSubscriptionFactory` - фабрика подписок
- `NatsConfig` - конфигурация транспорта

**Особенности:**
- Использование JetStream streams для durability
- Durable consumers для надежной доставки
- Поддержка заголовков сообщений
- Acknowledgment сообщений

## Как переключиться с RabbitMQ на NATS

### Шаг 1: Обновите конфигурацию

В файле конфигурации (appsettings.json или аналогичном) добавьте секцию для NATS:

```json
{
  "MessageBus": {
    "TransportType": "Nats",
    "Host": "nats-server",
    "Port": 4222,
    "StreamName": "SAL_STREAM"
  }
}
```

Или для RabbitMQ:

```json
{
  "MessageBus": {
    "TransportType": "RabbitMQ",
    "Host": "rabbitmq-server",
    "Port": 5672,
    "Username": "user",
    "Password": "password",
    "VirtualHost": "/"
  }
}
```

### Шаг 2: Обновите регистрацию в DI контейнере

В файле `ConfigureContainer.cs` замените регистрацию транспорта:

**Было (RabbitMQ):**
```csharp
builder.RegisterType<RabbitMQTransportAsync>()
    .Keyed<ITransport>(Contour.Front)
    .WithParameter("contour", Contour.Front)
    .SingleInstance();
```

**Стало (NATS):**
```csharp
builder.RegisterType<NatsJetStreamTransport>()
    .Keyed<ITransport>(Contour.Front)
    .WithParameter("host", Configuration["MessageBus:Host"])
    .WithParameter("port", int.Parse(Configuration["MessageBus:Port"]))
    .WithParameter("contour", Contour.Front)
    .SingleInstance();
```

Или используйте фабричный метод:

```csharp
builder.Register<IMessageTransport>(ctx =>
{
    var config = ctx.Resolve<IConfiguration>();
    var transportType = config["MessageBus:TransportType"];
    var contour = Contour.Front;
    var loggerProvider = ctx.Resolve<ILoggerProvider>();
    
    if (transportType == "Nats")
    {
        return new NatsJetStreamTransport(
            config["MessageBus:Host"],
            int.Parse(config["MessageBus:Port"]),
            contour.ToString(),
            loggerProvider);
    }
    else // RabbitMQ
    {
        return new RabbitMQTransportAsync(
            ctx.Resolve<IHostApplicationLifetime>(),
            ctx.Resolve<IConfigWatcher>(),
            loggerProvider,
            contour);
    }
})
.Keyed<ITransport>(Contour.Front)
.SingleInstance();
```

### Шаг 3: Убедитесь, что код использует интерфейсы

Весь ваш код должен использовать интерфейсы `IPublisher` и `ISubscriptionFactory`, а не конкретные реализации:

**Правильно:**
```csharp
public class MyService
{
    private readonly IPublisher _publisher;
    
    public MyService(ITransport transport)
    {
        _publisher = transport.CreatePublisher();
    }
}
```

**Неправильно:**
```csharp
public class MyService
{
    private readonly RabbitMQPublisher _publisher;
    
    public MyService(RabbitMQTransportAsync transport)
    {
        _publisher = new RabbitMQPublisher(transport, ...);
    }
}
```

## Ограничения NATS JetStream реализации

Следующие функции RabbitMQ не имеют полного аналога в NATS и могут требовать доработки:

1. **ExternalHttp subscriptions** - не поддерживаются, требуют RabbitMQ-specific features
2. **CommonSharedCommandResult** - ограниченная поддержка
3. **Топология очередей** - NATS использует другую модель (streams/consumers вместо queues/exchanges)

## Структура файлов

```
SAL.Core/
├── Transport/
│   └── IMessageTransport.cs          # Базовый интерфейс
├── Rabbit/
│   ├── RabbitMQTransportAsync.cs     # Реализация для RabbitMQ
│   ├── RabbitMQPublisher.cs          # Издатель RabbitMQ
│   ├── Interfaces/
│   │   ├── IRMQTransport.cs          # Расширенный интерфейс для RabbitMQ
│   │   ├── IPublisher.cs             # Интерфейс издателя
│   │   ├── ISubscription.cs          # Интерфейс подписки
│   │   ├── ISubscriptionFactory.cs   # Фабрика подписок
│   │   ├── RabbitMessage.cs          # Модель сообщения
│   │   └── RabbitMessageEx.cs        # Расширенная модель сообщения
│   └── Subscription/                 # Реализации подписок
└── Nats/
    ├── NatsJetStreamTransport.cs     # Реализация для NATS JetStream
    ├── NatsPublisher.cs              # Издатель NATS
    ├── NatsSubscription.cs           # Подписка NATS
    └── NatsSubscriptionFactory.cs    # Фабрика подписок NATS
```

## Пример использования

```csharp
// Получение транспорта из DI
var transport = container.Resolve<KeyedService<ITransport>>(Contour.Front);

// Запуск транспорта
transport.Start();

// Создание издателя
var publisher = transport.CreatePublisher();

// Публикация сообщения
await publisher.PublishAsync(new RabbitMessage
{
    Exchange = "sal.events",
    RoutingKey = "user.created",
    CorrelationId = Guid.NewGuid().ToString(),
    TimeStamp = DateTime.UtcNow,
    Payload = Encoding.UTF8.GetBytes("{\"userId\": 123}")
});

// Создание подписки
var subscriptionFactory = transport.CreateSubscriptionFactory();
var subscription = subscriptionFactory.CreateEvent(
    prefetchCount: 10,
    eventInfos: new[] { new EventInfo { EventName = "user.created" } },
    handler: async (msg, ack, nack) =>
    {
        // Обработка сообщения
        Console.WriteLine($"Received: {Encoding.UTF8.GetString(msg.Payload)}");
        ack();
    });

subscription.Start();

// Остановка
subscription.Stop();
transport.Stop();
```

## Миграция с RabbitMQ на NATS

1. **Анализ текущего использования**: Определите, какие функции RabbitMQ используются в вашем коде
2. **Тестирование**: Разверните NATS JetStream и протестируйте базовую функциональность
3. **Поэтапная миграция**: Начните с некритичных сервисов
4. **Мониторинг**: Отслеживайте производительность и надежность
5. **Откат**: Имейте план отката на RabbitMQ в случае проблем

## Дополнительные ресурсы

- [Документация NATS JetStream](https://docs.nats.io/nats-concepts/jetstream)
- [Документация RabbitMQ](https://www.rabbitmq.com/documentation.html)
- [Сравнение брокеров сообщений](https://medium.com/@devinrsmith/nats-vs-rabbitmq-a-comparison-8b6a6a3b6f3e)
