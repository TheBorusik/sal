using System;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    [SalEventName("AuthServer.ClientNotify")] 
    public class NotifyEvent : IEvent
    {
        public string EventName { get; set; }
        public JObject Payload { get; set; }
        public DateTime? TimeStamp { get; set; }
        
        public long[] RecipientAuthId { get; set; }
    }
}