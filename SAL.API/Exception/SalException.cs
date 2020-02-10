using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class SalException : System.Exception
    {
        internal readonly InternalExceptionDTO Dto;

        public string Code => Dto.Code;
        public string CodeDescription => Dto.CodeDescription;
        public DateTime TimeStamp => Dto.TimeStamp;
        public JObject Properties => Dto.Properties;
        public new string StackTrace => Dto.StackTrace;
        public string CallTrace => Dto.CallTrace;
        public string AdapterName => Dto.AdapterName;
        public string HandlerName => Dto.HandlerName;
        public string Sid => Dto.Sid;


        internal SalException(InternalExceptionDTO data) : base(data.Message)
        {
            this.Dto = data.DeepClone();
        }

        internal SalException(InternalExceptionDTO data, System.Exception innerException)
            : base(data.Message, innerException)
        {
            this.Dto = data.DeepClone();
        }
    }
}