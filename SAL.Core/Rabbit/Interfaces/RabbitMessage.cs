using System;

namespace SAL.Core.Rabbit.Interfaces
{
    public class RabbitMessage
    {
        
        // для отправки
        public string Exchange { get; set; }

        public string RoutingKey { get; set; }

        public string CorrelationId { get; set; }

        public byte Priority { get; set; }

        public DateTime TimeStamp { get; set; }

        public byte[] Payload { get; set; }
        // доп поля при получении

        public string QueueName { get; set; }
    }
}