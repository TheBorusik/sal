using System;
using Newtonsoft.Json.Linq;
using SAL.API;

namespace SAL.Core.DTO.Transport
{
    public class Message
    {
        public string CorrelationId { get; set; }
        public DateTime TimeStamp { get; set; }

        public string Source { get; set; }
        public string Destination { get; set; }
        public TimeSpan? TTL { get; set; }
        public byte Priority { get; set; }

        public JObject Session { get; set; }

        public string Type { get; set; }
        public JObject Payload { get; set; }

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
        public JObject Body { get; set; }
    }


    public class CommandPayload
    {
        public CommandDescriptor Descriptor { get; set; }
        public JObject Body { get; set; }
    }

    public class CommandResultPayload
    {
        public CommandResultDescriptor Descriptor { get; set; }
        public CommonCommandResult Body { get; set; }
    }
}
