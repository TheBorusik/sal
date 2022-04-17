using System.Threading.Tasks;

namespace SAL.API
{
    //CommandContext commandContext, ExecutingContext executingContext
    public interface ICommandHandler2
    {
    }
    
    public interface ICommandHandler2Async<in TCommand> : ICommandHandler2
        where TCommand : class, new()
    {
        Task Handle(TCommand command,CommandContext commandContext, ExecutingContext executingContext);
    }
    
}