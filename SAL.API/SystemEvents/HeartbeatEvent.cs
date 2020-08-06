using System;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;


namespace SAL.API
{
    [SalSystemEvent]
    [SalServiceType("System")]
    [SalEventName("HeartBit")]
    public class HeartbeatEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
    }
}