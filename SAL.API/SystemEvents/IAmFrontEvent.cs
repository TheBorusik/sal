using SAL.Infrastructure;

namespace SAL.API
{
    [SalEventName("System.IAmFront")]
    public class IAmFrontEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
        
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }
        
        public string AdapterHostName { get; set; }
        public string[] AdapterHostIp { get; set; }
        
        public bool InDocker { get; set; }
        
        public FrontCommandHandlerInfo[] CommandHandlers { get; set; }
        public CommandResultHandlerInfo[] CommandResultHandlers { get; set; }
        public EventHandlerInfo[] EventHandlers { get; set; }
        
        public string[] ExternalHttp { get; set; }
    }
}