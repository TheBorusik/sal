using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    public class CommandResult<TCommandResult> 
        where TCommandResult : class, ICommandResult, new()
    {
        public JObject Result { get; set; }
        public  InternalExceptionDTO Error { get; set; }
        public string ResultCode { get; set; }

        public TCommandResult GetResult() => Result.ConvertValue<TCommandResult>();

        public void SetResult(TCommandResult res)
        {
            Result = JObject.FromObject(res);
        }
        public T GetResult<T>() => Result.ConvertValue<T>();


        public CommandResult()
        {
        }

        public CommandResult(CommonCommandResult ccr)
        {
            Result = ccr.Result.Clone();
            Error = ccr.Error?.Clone();
            ResultCode = ccr.ResultCode;
        }

        public CommonCommandResult ToCommon()
        {
            return new CommonCommandResult
            {
                ResultCode = ResultCode,
                Error = Error?.Clone(),
                Result = Result.Clone()
            };
        }
        

        public CommandResult<TCommandResult> Clone()
        {
            return new CommandResult<TCommandResult>
            {
                ResultCode = ResultCode,
                Error = Error?.Clone(),
                Result = Result?.Clone()
            };
        }
    }
}