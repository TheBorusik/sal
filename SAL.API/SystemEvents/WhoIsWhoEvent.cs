using SAL.Infrastructure;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("WhoIsWho")]
    public class WhoIsWhoEvent : IEvent
    {
    }
}