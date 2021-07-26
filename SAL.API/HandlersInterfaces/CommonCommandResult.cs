using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;


namespace SAL.API
{
    public class CommonCommandResult 
    {
        public JObject Result { get; set; }
        public InternalExceptionDTO Error { get; set; }
        [Required]
        public string ResultCode { get; set; }
        
        public T GetResult<T>() => Result.ConvertValue<T>();
        
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