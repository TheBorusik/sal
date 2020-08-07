using System;

namespace SAL.API
{
    public class CommandResultDescriptor : CommandDescriptor
    {

        public string HandlerServiceType { get; set; }
        public string HandlerServiceName { get; set; }
        public TimeSpan HandlerDuration { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }


        public CommandResultDescriptor()
        {

        }
        public CommandResultDescriptor(CommandDescriptor commandDescriptor)
        {
            CorrelationId = commandDescriptor.CorrelationId;
            CommandName = commandDescriptor.CommandName;
            Priority = commandDescriptor.Priority;
            SourceAdapterType = commandDescriptor.SourceAdapterType;
            SourceAdapterName = commandDescriptor.SourceAdapterName;
            DestinationAdapterName = commandDescriptor.DestinationAdapterName;
            DestinationAdapterType = commandDescriptor.DestinationAdapterType;
            ResultAdaperType = commandDescriptor.ResultAdaperType;
            ResultAdaperName = commandDescriptor.ResultAdaperName;
            PublishTimeStamp = commandDescriptor.PublishTimeStamp;
            HandlerTimeStamp = commandDescriptor.HandlerTimeStamp;
            TTL = commandDescriptor.TTL;
            IsSync = commandDescriptor.IsSync;
        }
    }
}