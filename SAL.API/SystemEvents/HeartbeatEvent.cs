using System;
using SAL.Infrastructure;


namespace SAL.API
{
    [SalEventName("System.HeartBit")]
    public class HeartbeatEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }
        public string AdapterHostName { get; set; }
        public string[] AdapterHostIp { get; set; }
        
        public bool InDocker { get; set; }
        public DateTime Timestamp { get; set; }
    }
}