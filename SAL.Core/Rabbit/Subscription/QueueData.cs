using RabbitMQ.Client.Events;

namespace SAL.Core.Rabbit.Subscription
{
    internal class QueueData
    {
        public string QueueName;
        public string ConsumerTag;
        public EventingBasicConsumer Consumer;
        public ushort PrefetchCount;
    }
    
    internal class QueueDataAsync
    {
        public string QueueName;
        public string ConsumerTag;
        public AsyncEventingBasicConsumer Consumer;
        public ushort PrefetchCount;
    }
}