using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete("Use ICommandHandler2")]
    public interface ICommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }

    
    [Obsolete("Use ICommandHandler2Async")]
    public interface ICommandHandlerAsync<in TCommand, TCommandResult> : ICommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command);
    }
}