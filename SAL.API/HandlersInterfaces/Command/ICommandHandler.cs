using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete]
    public interface ICommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }

    
    [Obsolete]
    public interface ICommandHandlerAsync<in TCommand, TCommandResult> : ICommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command);
    }
}