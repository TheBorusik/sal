using System;
using SAL.Infrastructure;

namespace SAL.API
{
    public class EventDescriptor
    {
        public EventDescriptor()
        {
        }

        public EventDescriptor(EventDescriptor src)
        {
            CorrelationId = src.CorrelationId;
            EventName = src.EventName;
            DestinationAdapterType = src.DestinationAdapterType;
            DestinationAdapterName = src.DestinationAdapterName;
            SourceAdapterType = src.SourceAdapterType;
            SourceAdapterName = src.SourceAdapterName;
            PublishTimeStamp = src.PublishTimeStamp;
            TTL = src.TTL;
            IsCEvent = src.IsCEvent;
        }

        public string CorrelationId { get; set; }
        public string EventName { get; set; }
        public string DestinationAdapterType { get; set; }
        public string DestinationAdapterName { get; set; }
        
        public string Contour { get; set; }

        public bool IsSystem { get; set; }
        public string SourceAdapterType { get; set; }
        public string SourceAdapterName { get; set; }
        public DateTime PublishTimeStamp { get; set; }
        public TimeSpan? TTL { get; set; }
        public bool IsCEvent { get; set; }
    }
}