using System.Collections.Generic;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    public interface ICommandHandler
    {
        void SetContexts(CommandContext commandContext, ExecutingContext executingContext);
    }
    
    public interface IValidator<in TVerifiable> 
    {
        Task<IEnumerable<FieldError>> Validate(TVerifiable verifiable);
    }


    public interface ICommandHandlerAsync<in TCommand, TCommandResult> : ICommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command);
    }
}