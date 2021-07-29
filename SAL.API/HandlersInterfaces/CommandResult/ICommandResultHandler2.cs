using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    public interface ICommandResultHandler2
    {
    }
    
    public interface ICommandResultHandle2Async<TCommandResult> : ICommandResultHandler2
        where TCommandResult : class, new()
    {
        Task<bool> ResultHandle(CommandResult<TCommandResult> result, CommandResultContext commandContext, ExecutingContext executingContext);
    }
}