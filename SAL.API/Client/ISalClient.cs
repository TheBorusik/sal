using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    public interface ISalClient
    {

        public Contour Contour { get; }
        
        // hi level
        
        Task<string> PublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            string resultAdapterType = null,
            string resultAdapterName = null);
        
        Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommandResult>(
            string commandName,
            object commandBody,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            bool throwIfTimeout = true
        ) where TCommandResult : class, new();
        

        
        
        Task PublishResultAsync(object result, CommandContext commandContext);
        Task PublishResultAsync(CommonCommandResult result, CommandContext commandContext);
        Task PublishResultAsync(object result, string resultCode, CommandContext commandContext);
        Task PublishResultAsync(InternalExceptionDTO exceptionDto, CommandContext commandContext);
        Task PublishResultAsync(Exception exception, string errorCode, CommandContext commandContext);
        
        Task PublishEventAsync(
            string eventName, 
            object eventBody, 
            bool isSystem = false,
            TimeSpan? ttl = null, 
            string handlerAdapterType = null, 
            string handlerAdapterName = null);

        Task PublishCEventAsync(string eventName, object eventBody, string handlerAdapterType, TimeSpan? ttl = null);
        
        Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDto);
        
        /*
        [Obsolete]
        Task<SimpleCommandResult> ExecuteExternalHttp(
            ExternalHttpRequest request,
            string routePath,
            TimeSpan ttl,
            bool throwIfTimeout,
            string handlerAdapterType,
            string handlerAdapterName);
        
        [Obsolete]
        Task PublishResultAsync(ICommandResult result, CommandContext commandContext);
        [Obsolete]
        Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            string resultAdapterType = null,
            string resultAdapterName = null);
        
        
        [Obsolete]
        Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommand, TCommandResult>(
            TCommand command,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null
        )
            where TCommand : class, IHaveResult<TCommandResult>, new()
            where TCommandResult : class, ICommandResult, new();
        
        [Obsolete]
        Task PublishResultAsync(IList<FieldError> validationErrors, CommandContext commandContext);
        [Obsolete]
        Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandContext commandContext)
            where TCommandResult : class, ICommandResult, new();

        
        [Obsolete]
        Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null);
        [Obsolete]
        Task PublishCEventAsync(IEvent evnt, string handlerServiceType, TimeSpan? ttl = null);
        [Obsolete]
        Task RaiseExceptionDetectEvent(string cid, Exception ex);
        */
    }
}