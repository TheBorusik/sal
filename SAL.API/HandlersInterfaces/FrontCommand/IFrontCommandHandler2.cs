using System.Threading.Tasks;

namespace SAL.API
{
    public interface IFrontCommandHandler2
    {
        
    }
    
    public interface IFrontCommandHandler2Async<in TCommand, TResult> : IFrontCommandHandler2
    {
        Task Handle(TCommand command,CommandContext commandContext, ExecutingContext executingContext);
    }
}