using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit
{

    
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IRMQTransport transport;
        private readonly ILogger logger;
        
        private IModel rmqChannel;
        
        public RabbitMQPublisher(IRMQTransport transport, ILoggerProvider loggerProvider)
        {
            this.transport = transport;
            logger = loggerProvider.CreateLogger("RMQ.Publisher");
        }
        
        public  Task PublishAsync(RabbitMessage msg)
        {
            if(rmqChannel == null)
                throw new NoConnectionException();
            
            var props = rmqChannel.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.CorrelationId = msg.CorrelationId;
            props.Priority = msg.Priority;
            props.Timestamp = msg.TimeStamp.ToAmqp();
            rmqChannel.BasicPublish(msg.Exchange, msg.RoutingKey, props, msg.Payload);
            
            return Task.CompletedTask;
            
        }


        public void Start()
        {
            if (rmqChannel != null)
                Stop();

            rmqChannel = transport.CreateModel();

        

        }
        
        
        
        public void Stop()
        {
            rmqChannel.Dispose();
            rmqChannel = null;
        }




    }
}