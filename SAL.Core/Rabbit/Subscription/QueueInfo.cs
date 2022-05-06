namespace SAL.Core.Rabbit.Subscription
{
    public class QueueInfo
    {
        public string QueueName { get; set; }
        public ushort PrefetchCount { get; set; }
        
    }
    
    public class EventInfo
    {
        public string EventName { get; set; }
        public bool Preserved { get; set; }
        public bool OneInstance { get; set; }
    }
}