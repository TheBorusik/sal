using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class EventContext
    {
        public EventDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }

    }
}