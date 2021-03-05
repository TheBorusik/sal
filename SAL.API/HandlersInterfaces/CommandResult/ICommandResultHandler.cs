using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{

    public interface ICommandResultHandler
    {
        void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext);
    }

    public interface ICommandResultHandlerAsync<TCommand, TCommandResult> : ICommandResultHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task<bool> ResultHandle(CommandResult<TCommandResult> result);
    }
}