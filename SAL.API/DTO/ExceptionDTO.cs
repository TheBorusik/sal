using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class ExceptionDTO
    {
        public string Code { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public JObject Properties { get; set; }
        public string SessionId { get; set; }
        public string CorrelationId { get; set; }
        public string OperationId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }

    }
}
