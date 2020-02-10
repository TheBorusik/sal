using System;

namespace SAL.API
{
    public class CommandDescriptor
    {
        public string CorrelationId { get; set; }
        public string ServiceType { get; set; }
        public string ServiceName { get; set; }
        public string CommandName { get; set; }
        public CommandPriority Priority { get; set; }
        public string SourceServiceType { get; set; }
        public string SourceServiceName { get; set; }
        public string ResultServiceType { get; set; }
        public string ResultServiceName { get; set; }
        public DateTime PublishTimeStamp { get; set; }
        public DateTime? HandlerTimeStamp { get; set; }
        public TimeSpan? TTL { get; set; }
        public bool IsSync { get; set; }
    }
}