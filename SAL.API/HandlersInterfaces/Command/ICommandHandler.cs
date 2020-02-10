using System.Collections.Generic;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.Command
{
    public interface ICommandHandler
    {

    }
    
    public interface IValidator<in TVerifiable> 
    {
        new Task<IEnumerable<FieldError>> Validate(TVerifiable verifiable);
    }


    public interface ICommandHandlerAsync<in TCommand, TCommandResult> : ICommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task Handle(TCommand command, CommandContext context, ExecutingContext executingContext);
    }
}