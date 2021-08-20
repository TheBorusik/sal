using System;
using System.Threading.Tasks;

namespace SAL.API
{
    [Obsolete("Use ICommonCommandResultHandler2")]
    public interface ICommonCommandResultHandler : ICommandResultHandler
    {
        Task<bool> ResultHandle(CommonCommandResult result);
    }
    
    public interface ICommonCommandResultHandler2 : ICommandResultHandler2
    {
        Task<bool> ResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext);
    }
}