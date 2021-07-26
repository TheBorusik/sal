using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete]
    public interface ICommandResultHandler
    {
        void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext);
    }

    [Obsolete]
    public interface ICommandResultHandlerAsync<TCommand, TCommandResult> : ICommandResultHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task<bool> ResultHandle(CommandResult<TCommandResult> result);
    }
}