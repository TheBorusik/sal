using System;

namespace SAL.API
{
    public class CommandDescriptor
    {
        public string CorrelationId { get; set; }
        public string CommandName { get; set; }
        public string DestinationAdapterType{ get; set; }
        public string DestinationAdapterName { get; set; }
        public CommandPriority Priority { get; set; }
        public string SourceAdapterType { get; set; }
        public string SourceAdapterName { get; set; }
        public string ResultAdaperType { get; set; }
        public string ResultAdaperName { get; set; }
        public DateTime PublishTimeStamp { get; set; }
        public DateTime? HandlerTimeStamp { get; set; }
        public TimeSpan? TTL { get; set; }
        public bool IsSync { get; set; }
    }
}