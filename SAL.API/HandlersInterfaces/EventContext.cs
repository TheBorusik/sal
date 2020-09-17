using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class EventContext
    {
        public EventContext(){}

        public EventContext(EventContext src)
        {
            Descriptor = new EventDescriptor(src.Descriptor);
            Session = src.Session.Clone();
        }


        public EventDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }

    }
}