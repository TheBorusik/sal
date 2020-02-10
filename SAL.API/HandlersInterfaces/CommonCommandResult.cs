using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class CommonCommandResult 
    {
        public JObject Result { get; set; }
        public InternalExceptionDTO Error { get; set; }
        public string ResultCode { get; set; }
    }
}