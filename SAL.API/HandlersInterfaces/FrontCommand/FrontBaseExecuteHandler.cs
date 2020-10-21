using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.FrontCommand
{
    public abstract class FrontBaseExecuteHandler<TCommand, TCommandResult> : IFrontCommandHandlerAsync<TCommand, TCommandResult>
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        public abstract Task Handle(TCommand command);
        
        protected CommandContext commandContext;
        protected ExecutingContext executingContext;
        

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;

        }
        
        protected Task PublishResult(TCommandResult result)
        {
            return executingContext.SalClient.PublishResultAsync(result, commandContext.Descriptor);
        }

        protected Task PublishResult(InternalExceptionDTO error)
        {
            return executingContext.SalClient.PublishResultAsync(error, commandContext.Descriptor);
        }

        protected Task PublishError(
            string code,
            string message = null,
            object properties = null,
            Exception innerException = null)
        {
            return PublishResult(SalError.CreateDto(code, message, properties, innerException));
        }
    }
}