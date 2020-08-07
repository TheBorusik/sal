using System;

namespace SAL.API
{
    public class EventDescriptor
    {
        public string CorrelationId { get; set; }
        public string EventName { get; set; }
        public string DestinationAdapterType { get; set; }
        public string DestinationAdapterName { get; set; }

        public string SourceServiceType { get; set; }
        public string SourceServiceName { get; set; }
        public DateTime PublishTimeStamp { get; set; }

        public TimeSpan? TTL { get; set; }
    }
}