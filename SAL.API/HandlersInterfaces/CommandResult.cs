using SAL.Infrastructure;

namespace SAL.API
{
    public class CommandResult<TCommandResult> 
        where TCommandResult : class, ICommandResult, new()
    {
        public TCommandResult Result { get; set; }
        public  InternalExceptionDTO Error { get; set; }
        public string ResultCode { get; set; }
    }
}