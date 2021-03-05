using System.Threading.Tasks;

namespace SAL.API
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