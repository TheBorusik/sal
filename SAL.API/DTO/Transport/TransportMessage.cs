using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class TransportMessage
    {
        public string CorrelationId { get; set; }
        public DateTime TimeStamp { get; set; }

        public string Source { get; set; }
        public string Destination { get; set; }
        public TimeSpan? TTL { get; set; }
        public byte Priority { get; set; }
        
        public SessionInfo SessionInfo { get; set; }
        public string Type { get; set; }
        public JObject Payload { get; set; }

    }

    public class SessionInfo
    {
        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }
    }

    public static class MessageTypes
    {
        public const string Event = "Event";
        public const string Command = "Command";
        public const string CommandResult = "CommandResult";
    }


    public class EventPayload
    {
        public EventDescriptor Descriptor { get; set; }
        public JObject Payload { get; set; }
    }


    public class CommandPayload
    {
        public CommandDescriptor Descriptor { get; set; }
        public JObject Payload { get; set; }
    }

    public class CommandResultPayload
    {
        public CommandResultDescriptor Descriptor { get; set; }
        public CommonCommandResult Payload { get; set; }
    }
}
