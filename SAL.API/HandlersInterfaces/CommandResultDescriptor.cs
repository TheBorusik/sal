using System;

namespace SAL.API
{
    public class CommandResultDescriptor : CommandDescriptor
    {

        public string HandlerServiceType { get; set; }
        public string HandlerServiceName { get; set; }
        public TimeSpan HandlerDuration { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }


        public CommandResultDescriptor(CommandResultDescriptor src) :base(src)
        {
            HandlerServiceType = src.HandlerServiceType;
            HandlerServiceName = src.HandlerServiceName;
            HandlerDuration = src.HandlerDuration;
            ProcessingDuration = src.ProcessingDuration;
        }

        public CommandResultDescriptor() : base()
        {

        }
        public CommandResultDescriptor(CommandDescriptor commandDescriptor) : base()
        {
            CorrelationId = commandDescriptor.CorrelationId;
            CommandName = commandDescriptor.CommandName;
            Priority = commandDescriptor.Priority;
            SourceAdapterType = commandDescriptor.SourceAdapterType;
            SourceAdapterName = commandDescriptor.SourceAdapterName;
            DestinationAdapterName = commandDescriptor.DestinationAdapterName;
            DestinationAdapterType = commandDescriptor.DestinationAdapterType;
            ResultAdapterType = commandDescriptor.ResultAdapterType;
            ResultAdapterName = commandDescriptor.ResultAdapterName;
            PublishTimeStamp = commandDescriptor.PublishTimeStamp;
            HandlerTimeStamp = commandDescriptor.HandlerTimeStamp;
            TTL = commandDescriptor.TTL;
            IsSync = commandDescriptor.IsSync;
        }
    }
}