using System;

namespace SAL.API
{
    public class CommandResultDescriptor : CommandDescriptor
    {

        public string HandlerAdapterType { get; set; }
        public string HandlerAdatpterName { get; set; }
        
        public DateTime? PublishResultTimeStamp { get; set; }
        public TimeSpan HandlerDuration { get; set; }
        public DateTime? HandleResultTimeStamp { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        

        public CommandResultDescriptor(CommandResultDescriptor src) :base(src)
        {
            HandlerAdapterType = src.HandlerAdapterType;
            HandlerAdatpterName = src.HandlerAdatpterName;
            PublishResultTimeStamp = src.PublishResultTimeStamp;
            HandlerDuration = src.HandlerDuration;
            HandleResultTimeStamp = src.HandleResultTimeStamp;
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
            Contour = commandDescriptor.Contour;
        }
    }
}