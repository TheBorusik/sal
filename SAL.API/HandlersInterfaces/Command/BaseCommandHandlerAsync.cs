using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.Infrastructure;

namespace SAL.API
{
    public abstract class BaseCommandHandlerAsync<TCommand, TCommandResult> :
        ICommandHandler2Async<TCommand, TCommandResult>
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()

    {
        protected CommandContext commandContext;
        protected ISalClient salClient { get; set; }
        protected ILifetimeScope scope { get; set; }
        protected ILogger logger { get; set; }




        public abstract Task Handle(TCommand command);


        public Task PublishResult(TCommandResult result)
        {
            return salClient?.PublishResultAsync(result, commandContext);
        }

        public Task PublishResult(object result, string code)
        {
            return salClient?.PublishResultAsync(result, code, commandContext);
        }


        public Task PublishResult(InternalExceptionDTO error)
        {
            return salClient?.PublishResultAsync(error, commandContext);
        }

        public Task PublishError(string code,
            string message = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishResult(SalError.CreateDto(code, message, properties, innerException));
        }

        
        public Task Handle(TCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            salClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
            return Handle(command);
        }
    }
}