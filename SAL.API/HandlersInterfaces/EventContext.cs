using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class EventContext
    {
        public EventContext(){}

        public EventContext(EventContext src)
        {
            Descriptor = new EventDescriptor(src.Descriptor);
            SessionId = src.SessionId;
            AuthId = src.AuthId;
            ProcessId = src.ProcessId;
            OperationId = src.OperationId;
        }


        public EventDescriptor Descriptor { get; set; }
        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }
        public string OperationId { get; set; }

    }
}