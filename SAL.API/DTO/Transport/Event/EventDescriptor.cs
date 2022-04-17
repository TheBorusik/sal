using System;
using SAL.Infrastructure;

namespace SAL.API
{
    public class EventDescriptor
    {
        public string CorrelationId { get; set; }
        public string EventName { get; set; }
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public string Contour { get; set; }
        public string SourceAdapterType { get; set; }
        public string SourceAdapterName { get; set; }
        public DateTime PublishTimeStamp { get; set; }
        public TimeSpan? TTL { get; set; }
    }
}