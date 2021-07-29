using SAL.Infrastructure;

namespace SAL.API
{
    [SalEventName("System.IAmOffline")]
    public class IAmOffline : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}