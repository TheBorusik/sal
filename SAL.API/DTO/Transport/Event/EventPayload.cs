using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class EventPayload
    {
        public EventContext Context { get; set; }
        public JObject Payload { get; set; }
        
    }
}