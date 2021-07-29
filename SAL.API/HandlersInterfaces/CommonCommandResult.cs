using System;
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

        public static CommonCommandResult Create(object result)
        {
            return new()
            {
                ResultCode = ResultCodes.Success,
                Error = null,
                Result = JObject.FromObject(result)
            };
        }
        
        public static CommonCommandResult Create(Exception ex, string errorCode = SalErrorCodes.Fatal)
        {
            return new()
            {
                ResultCode = ResultCodes.Error,
                Error = ex.ToDto(errorCode),
                Result = null
            };   
        }
        
        public static CommonCommandResult Create(InternalExceptionDTO exDto)
        {
            return new()
            {
                ResultCode = ResultCodes.Error,
                Error = exDto.Clone(),
                Result = null
            };    
        }
        
        
    }
}