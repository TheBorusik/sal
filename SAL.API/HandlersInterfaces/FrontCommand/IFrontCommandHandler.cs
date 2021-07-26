using System;
using System.Threading.Tasks;

namespace SAL.API
{



    [Obsolete]
    public interface IFrontCommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }
    [Obsolete]
    public interface IFrontCommandHandlerAsync<in TCommand, TResult> : IFrontCommandHandler
    {
        Task Handle(TCommand command);
    }
}