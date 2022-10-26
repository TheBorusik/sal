using System;

namespace SAL.API
{
    public record CommandDescriptor
    {
        public string CorrelationId { get; init; }
        public string CommandName { get; init; }
        public string Version { get; init; }
        public string CommandExchangeName { get; init; }
        public string CommandRoutingKey { get; init; }
        public CommandPriority Priority { get; init; }
        public string SourceAdapterType { get; init; }
        public string SourceAdapterName { get; init; }
        public string ResultExchangeName { get; init; }
        public string ResultRoutingKey { get; init; }
        public DateTime PublishTimeStamp { get; init; }
        public DateTime? HandlerTimeStamp { get; set; }
        public TimeSpan? TTL { get; init; }
        public string Contour { get; set; }
        
    }
}