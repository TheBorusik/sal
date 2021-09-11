using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class SalException : System.Exception
    {
        internal readonly InternalExceptionDTO Dto;

        public string Code => Dto.Code;
        public DateTime TimeStamp => Dto.TimeStamp;
        public JObject Properties => Dto.Properties;
        public new string StackTrace => Dto.StackTrace;
        public string AdapterName => Dto.AdapterName;
        public string HandlerName => Dto.HandlerName;
        public string Sid => Dto.SessionId;
        public string Cid => Dto.CorrelationId;
        public string Oid => Dto.OperationId;
        public long? AuthId => Dto.AuthId;
        public long? ProcessId => Dto.ProcessId;


        internal SalException(InternalExceptionDTO data) : base(data.Message)
        {
            this.Dto = data.Clone();
        }

        internal SalException(InternalExceptionDTO data, System.Exception innerException)
            : base(data.Message, innerException)
        {
            this.Dto = data.Clone();
        }
    }
}