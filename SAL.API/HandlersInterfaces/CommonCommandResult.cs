using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    public class CommonCommandResult 
    {
        public JObject Result { get; set; }
        public InternalExceptionDTO Error { get; set; }
        public string ResultCode { get; set; }

        public CommonCommandResult Clone()
        {
            return new CommonCommandResult
            {
                Result = Result.Clone(),
                ResultCode = ResultCode,
                Error = Error?.Clone()
            };
        }
    }




}