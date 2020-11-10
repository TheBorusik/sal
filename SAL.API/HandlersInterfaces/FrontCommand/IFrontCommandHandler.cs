using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.FrontCommand
{



    public interface IFrontCommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }
    
    public interface IFrontCommandHandlerAsync<in TCommand, TResult> : IFrontCommandHandler
    {
        Task Handle(TCommand command);
    }
}