using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public abstract class BaseFrontBackCommandHandlerAsync<TCommand, TCommandResult> :
        ICommandHandler2Async<TCommand>,
        IFrontCommandHandler2Async<TCommand>,
        ICommandSchemeCreator,
        ICommandNameResolver
        where TCommand : class, new()
        where TCommandResult : class, new()
    {
        protected CommandContext commandContext;
        protected ISalClient salClient { get; set; }
        protected ILifetimeScope scope { get; set; }
        protected ILogger logger { get; set; }
        
        
        public abstract Task Handle(TCommand command);
        
        public Task PublishResult(TCommandResult result)
        {
            return salClient?.PublishResultAsync(result, ResultCodes.Success, commandContext);
        }
        public Task PublishResult(CommonCommandResult result)
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

        Task ICommandHandler2Async<TCommand>.Handle(TCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            salClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
            return Handle(command);
        }

        Task IFrontCommandHandler2Async<TCommand>.Handle(TCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            salClient = executingContext.SalClient;
            logger = executingContext.Logger;
            scope = executingContext.Scope;
            return Handle(command);
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
            if (handlerInterfaceType == typeof(IFrontCommandHandler2Async<TCommand>))
            {
                return GetType().GetAttribute<FrontCommandNameAttribute>()?.CommandName;
            }

            if (handlerInterfaceType == typeof(ICommandHandler2Async<TCommand>))
            {
                return  GetType().GetAttribute<BackCommandNameAttribute>()?.CommandName;
            }

            return null;
        }
    }

    public class BackCommandNameAttribute : Attribute
    {
        public string CommandName { get; private set; }

        public BackCommandNameAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
    
    public class FrontCommandNameAttribute : Attribute
    {
        public string CommandName { get; private set; }

        public FrontCommandNameAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}