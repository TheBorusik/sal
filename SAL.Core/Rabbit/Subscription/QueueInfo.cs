namespace SAL.Core.Rabbit.Subscription
{
    public class QueueInfo
    {
        public string QueueName { get; set; }
        public ushort PrefetchCount { get; set; }
    }
}