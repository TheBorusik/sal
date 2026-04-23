using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using Newtonsoft.Json;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Nats
{
    /// <summary>
    /// Издатель сообщений для NATS JetStream
    /// Реализует интерфейс IPublisher
    /// </summary>
    public class NatsPublisher : IPublisher
    {
        private readonly IJetStream jetStream;
        private readonly ILogger logger;
        private readonly string contourName;
        private readonly string streamName;

        public NatsPublisher(IJetStream jetStream, ILogger logger, string contourName)
        {
            this.jetStream = jetStream;
            this.logger = logger;
            this.contourName = contourName;
            this.streamName = $"SAL_{contourName.ToUpper()}_STREAM";
        }

        public async Task PublishAsync(RabbitMessage message)
        {
            try
            {
                // Формируем subject на основе routing key
                var subject = message.RoutingKey ?? "sal.message";
                
                // Создаем заголовки
                var headers = new MsgHeader();
                headers["correlation_id"] = message.CorrelationId;
                headers["timestamp"] = message.TimeStamp.ToString("O");
                headers["priority"] = message.Priority.ToString();
                headers["exchange"] = message.Exchange ?? "";
                
                // Сериализуем payload
                var data = message.Payload ?? Encoding.UTF8.GetBytes("{}");
                
                // Публикуем сообщение в JetStream
                var msg = new Msg(subject, data);
                msg.Headers = headers;
                
                var ack = await jetStream.PublishAsync(msg);
                
                logger.LogDebug($"Published message to {subject}, seq: {ack.Seq}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to publish message to {message.RoutingKey}");
                throw;
            }
        }
    }
}
