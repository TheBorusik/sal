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
        public string Sid { get; set; }

    }


    public class InternalExceptionDTO : ExceptionDTO
    {
        public string StackTrace { get; set; }
        public string CallTrace { get; set; }
        public string AdapterName { get; set; }
        public string HandlerName { get; set; }
        public string ExceptionType { get; set; }


        public InternalExceptionDTO InnerException { get; set; }

        public InternalExceptionDTO DeepClone()
        {
            return new InternalExceptionDTO
            {
                Code = Code,
                TimeStamp = TimeStamp,
                Message = Message,
                Properties = (JObject)Properties.DeepClone(),
                Sid = Sid,
                StackTrace = StackTrace,
                CallTrace = CallTrace,
                AdapterName = AdapterName,
                HandlerName = HandlerName,
                InnerException = InnerException?.DeepClone()
            };
        }
    }
}
