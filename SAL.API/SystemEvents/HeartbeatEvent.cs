using System;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;


namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("HeartBit")]
    public class HeartbeatEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }
        public DateTime Timestamp { get; set; }
    }
}