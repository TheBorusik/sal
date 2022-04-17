using System;

namespace SAL.API
{
    public record CommandResultDescriptor : CommandDescriptor
    {

        public CommandResultDescriptor()
        {}
        public CommandResultDescriptor(CommandDescriptor src)
        {
            CorrelationId = src.CorrelationId;
            CommandName = src.CommandName;
            CommandExchangeName = src.CommandExchangeName;
            CommandRoutingKey = src.CommandRoutingKey;
            Priority = src.Priority;
            SourceAdapterType = src.SourceAdapterType;
            SourceAdapterName = src.SourceAdapterName;
            ResultExchangeName = src.ResultExchangeName;
            ResultRoutingKey = src.ResultRoutingKey;
            PublishTimeStamp = src.PublishTimeStamp;
            HandlerTimeStamp = src.HandlerTimeStamp;
            TTL = src.TTL;
            Contour = src.Contour;
        }
        
        public string HandlerAdapterType { get; set; }
        public string HandlerAdapterName { get; set; }
        public DateTime? PublishResultTimeStamp { get; set; }
        public TimeSpan HandlerDuration { get; set; }
        
        public DateTime? HandleResultTimeStamp { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        
    }
}