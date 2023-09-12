using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class InternalExceptionDTO : ExceptionDTO
    {
        public string StackTrace { get; set; }
        public string AdapterName { get; set; }
        public string HandlerName { get; set; }
        public string ExceptionType { get; set; }

        public InternalExceptionDTO InnerException { get; set; }

        public InternalExceptionDTO Clone()
        {
            return new InternalExceptionDTO
            {
                Code = Code,
                TimeStamp = TimeStamp,
                Message = Message,
                Properties = Properties.Clone(),
                SessionId = SessionId,
                CorrelationId = CorrelationId,
                OperationId = OperationId,
                ProcessId = ProcessId,
                AuthId = AuthId,
                StackTrace = StackTrace,
                AdapterName = AdapterName,
                HandlerName = HandlerName,
                InnerException = InnerException?.Clone(),
                ExceptionType = ExceptionType,
            };
        }

        public InternalExceptionDTO ClearTrace()
        {
            StackTrace = null;
            return this;
        }
    }

    public class LogExceptionDTO
    {
        public string Code { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public JObject Properties { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionType { get; set; }
        public LogExceptionDTO InnerException { get; set; }
    }
}