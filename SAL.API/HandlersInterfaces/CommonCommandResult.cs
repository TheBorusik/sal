using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    public class CommonCommandResult : ICommandResult
    {
        public JObject Result { get; set; }
        public InternalExceptionDTO Error { get; set; }
        public string ResultCode { get; set; }
    }
}