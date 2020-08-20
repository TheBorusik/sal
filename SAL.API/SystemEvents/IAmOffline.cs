using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("IAmOffline")]
    public class IAmOffline : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}