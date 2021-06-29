using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class EventContext
    {
        public EventContext(){}

        public EventContext(EventContext src)
        {
            Descriptor = new EventDescriptor(src.Descriptor);
            ContextInfo = new ContextInfo(src.ContextInfo);
        }


        public EventDescriptor Descriptor { get; set; }
        public ContextInfo ContextInfo { get; set; }

    }
}