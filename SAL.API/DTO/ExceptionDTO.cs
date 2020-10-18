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
}
