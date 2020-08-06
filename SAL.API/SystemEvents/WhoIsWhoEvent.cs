using System;
using System.Collections.Generic;
using System.Text;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalServiceType("System")]
    [SalEventName("WhoIsWho")]
    public class WhoIsWhoEvent : IEvent
    {
    }
}