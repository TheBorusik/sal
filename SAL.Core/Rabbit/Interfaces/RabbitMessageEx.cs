using Newtonsoft.Json.Linq;

namespace SAL.Core.Rabbit.Interfaces
{
    public class RabbitMessageEx : RabbitMessage
    {
        public JObject Headers { get; set; }
        public bool Redelivered { get; set; }

    }
}