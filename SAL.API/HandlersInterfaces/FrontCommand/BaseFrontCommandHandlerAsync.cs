using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;

namespace SAL.API
{
    public abstract class BaseFrontCommandHandlerAsync<TCommand, TCommandResult> : 
        IFrontCommandHandlerAsync<TCommand,TCommandResult>,
        IValidator<TCommand>
    {
        protected CommandContext commandContext;

        protected BaseFrontCommandHandlerAsync(ISalClient backClient)
        {
            this.backClient = backClient;
        }

        protected ISalClient backClient { get; set; }
        protected ISalClient frontClient { get; set; }
        protected ILifetimeScope scope { get; set; }
        protected ILogger logger { get; set; }

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            frontClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
        }


        public virtual Task<IEnumerable<FieldError>> Validate(TCommand verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }
        
        public abstract Task Handle(TCommand command);


        public Task PublishSuccess(TCommandResult result)
        {
            return frontClient?.PublishResultAsync(result, ResultCodes.Success, commandContext.Descriptor);
        }
        
        public Task PublishResult(object result, string code)
        {
            return frontClient?.PublishResultAsync(result, code, commandContext.Descriptor);
        }
        public Task PublishError(InternalExceptionDTO error)
        {
            return frontClient?.PublishResultAsync(error, commandContext.Descriptor);
        }

        public Task PublishError(string code,
            string message = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishError(SalError.CreateDto(code, message, properties, innerException));
        }
        
    }
    
}