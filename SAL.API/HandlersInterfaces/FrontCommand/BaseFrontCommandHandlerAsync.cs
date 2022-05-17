using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public abstract class BaseFrontCommandHandlerAsync<TCommand, TCommandResult> : 
        IFrontCommandHandler2Async<TCommand>,
        ICommandSchemeCreator,
        ICommandNameResolver
    {
        protected CommandContext commandContext;

        
        protected ISalClient frontClient { get; set; }
        protected ILifetimeScope scope { get; set; }
        protected ILogger logger { get; set; }
        
        
        public abstract Task Handle(TCommand command);

        public Task Handle(TCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            frontClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
            return Handle(command);
        }
        
        public Task PublishResult(TCommandResult result)
        {
            return frontClient?.PublishResultAsync(result, ResultCodes.Success, commandContext);
        }

        public Task PublishResult(CommonCommandResult result)
        {
            return frontClient?.PublishResultAsync(result, commandContext);
        }
        
        public Task PublishResult(object result, string code)
        {
            return frontClient?.PublishResultAsync(result, code, commandContext);
        }
        
        public Task PublishResult(InternalExceptionDTO error)
        {
            return frontClient?.PublishResultAsync(error, commandContext);
        }
        
        
        public Task PublishError(string code,
            string message = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishResult(SalError.CreateDto(code, message, properties, innerException));
        }


        public virtual JSchema GetCommandSchema(string commandName)
        {
            return SalSchema.Generate(typeof(TCommand));
        }

        public virtual JSchema GetResultSchema(string commandName)
        {
            return SalSchema.Generate(typeof(TCommandResult));
        }

        public virtual string Resolve(Type handlerInterfaceType)
        {
            return GetType().GetAttribute<FrontCommandNameAttribute>()?.CommandName;
        }
    }
    
}