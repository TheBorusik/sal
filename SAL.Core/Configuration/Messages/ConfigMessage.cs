using System;
using Newtonsoft.Json.Linq;

namespace SAL.Core.Configuration.Messages
{
    public class ConfigMessage
    {
        public MessageTypes Type { get; set; }
        public string CorrelationId { get; set; }
        public string Source { get; set; }
        public string[] Destination { get; set; }
        public JObject Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum MessageTypes
    {
        None,
        Error,
        GetAdapterName,
        ConfigChanged,
    }
    
    public class Error
    {
        public string Code { get; set; }
    }
    
    public static class ConfigErrorCodes
    {
        public const string Timeout = "Timeout";
    }
    
    public class GetAdapterNameReq
    {
        public string AdapterType { get; set; }
        public string MachineName { get; set; }
        public bool InDocker { get; set; }
        public string TaskSlot { get; set; }
    }

    public class GetAdapterNameRes
    {
        public string AdapterName { get; set; }
    }
    
    public class ConfigChanged
    {
    }
    
}