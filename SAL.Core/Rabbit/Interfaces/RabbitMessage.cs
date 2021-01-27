using System;
using Newtonsoft.Json.Linq;

namespace SAL.Core.Rabbit.Interfaces
{
    public class RabbitMessage
    {
        
        // для отправки
        public string RoutingKey { get; set; }

        public string CorrelationId { get; set; }

        public byte Priority { get; set; }

        public DateTime TimeStamp { get; set; }

        public ReadOnlyMemory<byte> Payload { get; set; }
        // доп поля при получении

        public string QueueName { get; set; }

        // заполняеться при использовании того или иного метода
        public string Exchange { get; set; }

    }

    public class RabbitMessageEx : RabbitMessage
    {
        public JObject Headers { get; set; }
        public bool Redelivered { get; set; }

    }
}