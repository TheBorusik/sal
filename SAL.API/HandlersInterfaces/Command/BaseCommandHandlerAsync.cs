using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.Command
{
    public abstract class BaseCommandHandlerAsync<TCommand, TCommandResult> : ICommandHandlerAsync<TCommand, TCommandResult>
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        private CommandContext commandContext;
        private ExecutingContext executingContext;



        public Task Handle(TCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;

            return Handle(command);
        }

        public abstract Task Handle(TCommand command);


        public Task PublishResult(TCommandResult result)
        {
            return executingContext.SalClient?.PublishResultAsync(result, commandContext.Descriptor);
        }

        /*
        public Task PublishError(InternalExceptionDTO error)
        {
            return executingContext.SalClient?.P(error, commandContext.Descriptor);
            return Task.CompletedTask;
        }

        public Task PublishError(string code,
            string message = null,
            string codeDescription = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishError(SalError.CreateDto(code, message, codeDescription, properties, innerException));
        }
        */

    }
}