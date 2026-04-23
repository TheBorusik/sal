using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client;
using NATS.Client.JetStream;
using SAL.Core.Rabbit.Interfaces;
using EventInfo = SAL.Core.Rabbit.Subscription.EventInfo;

namespace SAL.Core.Nats
{
    /// <summary>
    /// Подписка на сообщения для NATS JetStream
    /// Реализует интерфейс ISubscription
    /// </summary>
    public class NatsSubscription : ISubscription
    {
        private readonly IJetStream jetStream;
        private readonly ILogger logger;
        private readonly string contourName;
        private readonly string subscriptionName;
        private readonly IEnumerable<EventInfo> eventInfos;
        private readonly Func<RabbitMessage, Action, Action, Task> handler;
        private readonly Func<RabbitMessageEx, Action, Action, Task> customHandler;
        
        private IJetStreamPushAsyncSubscription subscription;
        private bool disposed;
        private CancellationTokenSource cts;

        public NatsSubscription(
            IJetStream jetStream, 
            ILogger logger, 
            string contourName, 
            string subscriptionName,
            IEnumerable<EventInfo> eventInfos,
            Func<RabbitMessage, Action, Action, Task> handler,
            Func<RabbitMessageEx, Action, Action, Task> customHandler = null)
        {
            this.jetStream = jetStream;
            this.logger = logger;
            this.contourName = contourName;
            this.subscriptionName = subscriptionName;
            this.eventInfos = eventInfos;
            this.handler = handler;
            this.customHandler = customHandler;
        }

        public void Start()
        {
            if (subscription != null)
            {
                logger.LogWarning($"Subscription {subscriptionName} is already started");
                return;
            }

            try
            {
                cts = new CancellationTokenSource();
                
                // Определяем subjects для подписки
                var subjects = GetSubjects();
                
                foreach (var subject in subjects)
                {
                    var streamName = $"SAL_{contourName.ToUpper()}_STREAM";
                    
                    // Создаем или получаем consumer
                    var consumerConfig = new ConsumerConfiguration
                    {
                        DurableName = $"{subscriptionName}_{contourName}",
                        DeliverSubject = $"{subject}.deliver",
                        AckPolicy = AckPolicy.Explicit,
                        MaxAckPending = 100,
                        FilterSubject = subject
                    };

                    subscription = jetStream.CreatePushAsyncConsumer(streamName, consumerConfig);
                    
                    // Подписываемся на сообщения
                    _ = ProcessMessagesAsync(cts.Token);
                    
                    logger.LogInformation($"Started NATS subscription for {subject}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to start subscription {subscriptionName}");
                throw;
            }
        }

        public void Stop()
        {
            if (disposed) return;
            
            try
            {
                cts?.Cancel();
                subscription?.Dispose();
                subscription = null;
                
                logger.LogInformation($"Stopped subscription {subscriptionName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while stopping subscription {subscriptionName}");
            }
        }

        private List<string> GetSubjects()
        {
            var subjects = new List<string>();
            
            if (eventInfos != null)
            {
                foreach (var eventInfo in eventInfos)
                {
                    subjects.Add($"sal.{contourName.ToLower()}.{eventInfo.EventName.ToLower()}");
                }
            }
            else
            {
                // Общий subject для команд и результатов
                subjects.Add($"sal.{contourName.ToLower()}.{subscriptionName.ToLower()}.*");
            }
            
            return subjects;
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var msg in subscription.GetMessagesAsync(cancellationToken))
                {
                    try
                    {
                        var rabbitMessage = ConvertToRabbitMessage(msg);
                        
                        if (handler != null)
                        {
                            await handler(rabbitMessage, 
                                () => msg.Ack(), 
                                () => msg.Nak());
                        }
                        else if (customHandler != null)
                        {
                            var rabbitMessageEx = new RabbitMessageEx
                            {
                                Exchange = msg.Headers?["exchange"] ?? "",
                                RoutingKey = msg.Subject,
                                CorrelationId = msg.Headers?["correlation_id"] ?? "",
                                Priority = byte.Parse(msg.Headers?["priority"] ?? "0"),
                                TimeStamp = DateTime.Parse(msg.Headers?["timestamp"] ?? DateTime.UtcNow.ToString("O")),
                                Payload = msg.Data,
                                QueueName = subscription.ConsumerInfo.Name,
                                Headers = msg.Headers != null ? 
                                    new Newtonsoft.Json.Linq.JObject(
                                        new Newtonsoft.Json.Linq.JProperty("headers", 
                                            new Newtonsoft.Json.Linq.JValue(msg.Headers.ToString()))) 
                                    : null,
                                Redelivered = false
                            };
                            
                            await customHandler(rabbitMessageEx,
                                () => msg.Ack(),
                                () => msg.Nak());
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing message from {msg.Subject}");
                        msg.Nak();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug($"Message processing cancelled for {subscriptionName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in message processing loop for {subscriptionName}");
            }
        }

        private RabbitMessage ConvertToRabbitMessage(Msg msg)
        {
            return new RabbitMessage
            {
                Exchange = msg.Headers?["exchange"] ?? "",
                RoutingKey = msg.Subject,
                CorrelationId = msg.Headers?["correlation_id"] ?? "",
                Priority = byte.Parse(msg.Headers?["priority"] ?? "0"),
                TimeStamp = DateTime.Parse(msg.Headers?["timestamp"] ?? DateTime.UtcNow.ToString("O")),
                Payload = msg.Data,
                QueueName = subscription?.ConsumerInfo?.Name ?? ""
            };
        }

        public void Dispose()
        {
            if (disposed) return;
            
            Stop();
            cts?.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
