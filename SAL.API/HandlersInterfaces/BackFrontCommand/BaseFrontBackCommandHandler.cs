using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API.Command;
using SAL.API.FrontCommand;
using SAL.Infrastructure;

namespace SAL.API
{
    public abstract class BaseFrontBackCommandHandlerAsync<TCommand, TCommandResult> :
        ICommandHandlerAsync<TCommand, TCommandResult>,
        IFrontCommandHandlerAsync<TCommand,TCommandResult>,
        IValidator<TCommand>
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        protected CommandContext commandContext;
        protected ISalClient salClient { get; set; }
        protected ILifetimeScope scope { get; set; }
        protected ILogger logger { get; set; }

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            salClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
        }

        public virtual Task<IEnumerable<FieldError>> Validate(TCommand verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }  
        

        public abstract Task Handle(TCommand command);
        
        public Task PublishResult(TCommandResult result)
        {
            return salClient?.PublishResultAsync(result, commandContext.Descriptor);
        }

        public Task PublishResult(object result, string code)
        {
            return salClient?.PublishResultAsync(result, code, commandContext.Descriptor);
        }
        
        public Task PublishResult(InternalExceptionDTO error)
        {
            return salClient?.PublishResultAsync(error, commandContext.Descriptor);
        }

        public Task PublishError(string code,
            string message = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishResult(SalError.CreateDto(code, message, properties, innerException));
        }

    }
}