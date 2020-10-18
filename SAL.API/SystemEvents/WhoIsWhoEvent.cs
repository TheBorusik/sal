using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("WhoIsWho")]
    public class WhoIsWhoEvent : IEvent
    {
    }
}