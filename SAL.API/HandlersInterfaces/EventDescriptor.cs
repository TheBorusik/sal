using System;

namespace SAL.API
{
    public class EventDescriptor
    {
        public string CorrelationId { get; set; }
        public string ServiceType { get; set; }
        public string ServiceName { get; set; }
        public string EventName { get; set; }

        public string SourceServiceType { get; set; }
        public string SourceServiceName { get; set; }
        public DateTime PublishTimeStamp { get; set; }

        public TimeSpan? TTL { get; set; }
    }
}