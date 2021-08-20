using System;
using System.Threading.Tasks;

namespace SAL.API
{



    [Obsolete("Use IFrontCommandHandler2")]
    public interface IFrontCommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }
    [Obsolete("Use IFrontCommandHandler2Async")]
    public interface IFrontCommandHandlerAsync<in TCommand, TResult> : IFrontCommandHandler
    {
        Task Handle(TCommand command);
    }
}