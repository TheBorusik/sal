using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete("Use ICommandResultHandler2")]
    public interface ICommandResultHandler
    {
        void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext);
    }

    [Obsolete("Use ICommandResultHandler2Async")]
    public interface ICommandResultHandlerAsync<TCommand, TCommandResult> : ICommandResultHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task<bool> ResultHandle(CommandResult<TCommandResult> result);
    }
}