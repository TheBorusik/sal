using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    //CommandContext commandContext, ExecutingContext executingContext
    public interface ICommandHandler2
    {
    }
    
    public interface ICommandHandler2Async<in TCommand, TCommandResult> : ICommandHandler2
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command,CommandContext commandContext, ExecutingContext executingContext);
    }
}