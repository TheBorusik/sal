using System.Collections.Generic;
using System.Threading.Tasks;
using SAL.API.Command;
using SAL.Infrastructure;

namespace SAL.API.FrontCommand
{



    public interface IFrontCommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }
    

    public interface IFrontCommandHandlerAsync<in TCommand, TCommandResult> : IFrontCommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command);
    }

    
}