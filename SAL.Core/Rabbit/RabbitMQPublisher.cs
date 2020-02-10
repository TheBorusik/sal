using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit
{
    public class RabbitMQPublisher : IPublisher
    {

        private readonly RabbitMQTransport transport;
        private readonly ILogger logger;

        public RabbitMQPublisher(RabbitMQTransport transport, ILoggerProvider loggerProvider)
        {
            this.transport = transport;
            this.logger = loggerProvider.CreateLogger("RMQ.Publisher");
        }
        public void PublishEvent(RabbitMessage msg)
        {

            msg.Priority = 0;
            msg.Exchange = ExchangeNames.EventExchange;

            Publish(msg);
        }

        public void PublishCommand(RabbitMessage msg)
        {

            /*
            var needUpdateTopology = false;

            if (!string.IsNullOrWhiteSpace(msg.QueueName))
            {
                if (!transport.IsQueuePresent(msg.QueueName))
                {
                    transport.AddQueue(new Queue
                    {
                        Name = msg.QueueName,
                        AutoDelete = false,
                        MaxPriority = 9,
                        Exclusive = false,
                        HasDeadLetter = true,
                        Expire = null,
                        Durable = true,
                        Bindings = new[]{ new QueueBinding
                            {
                                ExchangeName = ExchangeNames.CommandExchange,
                                RoutingKey = msg.RoutingKey,
                            }}
                    });
                    needUpdateTopology = true;
                }-

            }

            if (needUpdateTopology)
                transport.UpdateTopology();
                */

            msg.Exchange = ExchangeNames.CommandExchange;

            Publish(msg);
        }
        public void PublishCommandResult(RabbitMessage msg)
        {

            msg.Exchange = ExchangeNames.CommandResultExchange;

            Publish(msg);
        }
        private void Publish(RabbitMessage msg)
        {

            using (var model = transport.CreateModel())
            {
                var routingKey = msg.RoutingKey ?? string.Empty;

                var props = model.CreateBasicProperties();

                props.DeliveryMode = 2;

                props.CorrelationId = msg.CorrelationId;

                props.Priority = msg.Priority;

                props.Timestamp = msg.TimeStamp.ToAmqp();

                model.BasicPublish(msg.Exchange, routingKey, props, msg.Payload);
            }
        }
    }
}