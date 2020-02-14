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
            ServiceType = commandDescriptor.ResultServiceType;
            ServiceName = commandDescriptor.ResultServiceName;
            CommandName = commandDescriptor.CommandName;
            Priority = commandDescriptor.Priority;
            SourceServiceType = commandDescriptor.SourceServiceType;
            SourceServiceName = commandDescriptor.SourceServiceName;
            ResultServiceType = commandDescriptor.ResultServiceType;
            ResultServiceName = commandDescriptor.ResultServiceName;
            PublishTimeStamp = commandDescriptor.PublishTimeStamp;
            HandlerTimeStamp = commandDescriptor.HandlerTimeStamp;
            TTL = commandDescriptor.TTL;
            IsSync = commandDescriptor.IsSync;
        }
    }
}