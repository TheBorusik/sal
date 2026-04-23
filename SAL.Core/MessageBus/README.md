# Архитектура шины сообщений (Message Bus Abstraction)

## Обзор

Данный модуль предоставляет абстрактный интерфейс для работы с различными транспортными системами обмена сообщениями. 
В текущей реализации поддерживаются:
- **RabbitMQ** (существующая инфраструктура)
- **NATS JetStream** (новая альтернатива)

## Структура

```
SAL.Core/MessageBus/
├── Abstraction/          # Базовые интерфейсы и модели
│   └── IMessageBus.cs    # Основные интерфейсы: IMessageBus, IMessagePublisher, IMessageSubscriber
├── RabbitMQ/             # Реализация для RabbitMQ
│   └── RabbitMQMessageBus.cs
└── Nats/                 # Реализация для NATS JetStream
    └── NatsJetStreamMessageBus.cs
```

## Основные интерфейсы

### IMessageBus
Главный интерфейс транспортной шины:
- `Start()` / `Stop()` - управление жизненным циклом
- `IsConnected` - статус подключения
- `CreatePublisher()` - создание издателя
- `CreateSubscriberFactory()` - создание фабрики подписчиков
- События `ConnectionRestored` / `ConnectionFailed`

### IMessagePublisher
Интерфейс для публикации сообщений:
```csharp
Task PublishAsync(string exchangeOrSubject, Message message);
```

### IMessageSubscriber
Интерфейс для подписки на сообщения:
```csharp
void Start();
void Stop();
```

### Message
Базовый класс сообщения с полями:
- `Id` - уникальный идентификатор
- `CorrelationId` - ID корреляции
- `ReplyTo` - адрес для ответа
- `Timestamp` - время создания
- `Payload` - тело сообщения (byte[])
- `ContentType` - тип содержимого

## Использование

### Пример с RabbitMQ

```csharp
// Регистрация в DI контейнере
services.AddSingleton<IMessageBus>(sp =>
{
    var transport = sp.GetRequiredService<ITransport>();
    var logger = sp.GetRequiredService<ILogger<RabbitMQMessageBus>>();
    return new RabbitMQMessageBus(transport, logger);
});

// Использование
var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
messageBus.Start();

var publisher = messageBus.CreatePublisher();
await publisher.PublishAsync("my.exchange", new Message 
{ 
    Payload = Encoding.UTF8.GetBytes("{\"data\":\"value\"}") 
});
```

### Пример с NATS JetStream

```csharp
// Регистрация в DI контейнере
services.AddSingleton<IMessageBus>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var jetStream = connection.CreateJetStreamContext();
    var logger = sp.GetRequiredService<ILogger<NatsJetStreamMessageBus>>();
    return new NatsJetStreamMessageBus(connection, jetStream, "SAL_STREAM", logger);
});

// Использование аналогично RabbitMQ
var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
messageBus.Start();

var publisher = messageBus.CreatePublisher();
await publisher.PublishAsync("events.user.created", new Message 
{ 
    Payload = Encoding.UTF8.GetBytes("{\"userId\":123}") 
});
```

## Миграция с RabbitMQ на NATS JetStream

### Шаг 1: Обновление конфигурации
Замените переменные окружения для RabbitMQ на NATS:

**Было (RabbitMQ):**
```bash
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=user
RABBITMQ_PASSWORD=pass
```

**Стало (NATS):**
```bash
NATS_URL=nats://localhost:4222
NATS_STREAM_NAME=SAL_STREAM
```

### Шаг 2: Обновление регистрации в DI
Измените регистрацию сервиса в `Startup.cs` или `Program.cs`:

**Было:**
```csharp
services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
```

**Стало:**
```csharp
services.AddSingleton<IMessageBus, NatsJetStreamMessageBus>();
```

### Шаг 3: Тестирование
Протестируйте публикацию и подписку на сообщения через новый транспорт.

## Преимущества архитектуры

1. **Независимость от транспорта** - бизнес-код не зависит от конкретной реализации
2. **Легкая замена** - переключение между RabbitMQ и NATS требует минимальных изменений
3. **Тестируемость** - возможность создания mock-реализаций для тестов
4. **Расширяемость** - простая добавление новых транспортов (Kafka, Azure Service Bus и т.д.)

## Добавление нового транспорта

Для добавления нового транспорта:
1. Создайте папку в `SAL.Core/MessageBus/<NewTransport>/`
2. Реализуйте интерфейс `IMessageBus`
3. Зарегистрируйте реализацию в DI контейнере

## Примечания

- Текущая реализация NATS JetStream является демонстрационной и может требовать доработки под конкретные нужды
- Для production-использования рекомендуется добавить обработку ошибок, retry-логику и мониторинг
- Поддержка JetStream требует NATS Server версии 2.2.0 или выше
