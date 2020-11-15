using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("IAmFront")]
    public class IAmFrontEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
        
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }

        public FrontCommandHandlerInfo[] CommandHandlers { get; set; }
        public CommandResultHandlerInfo[] CommandResultHandlers { get; set; }
        public EventHandlerInfo[] EventHandlers { get; set; }
        public string[] ExternalHttp { get; set; }
    }
}