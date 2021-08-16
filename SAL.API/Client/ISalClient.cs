using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    public interface ISalClient
    {

        public Contour Contour { get; }
        
        // hi level
        Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null,
            bool typeHandler = false
        )
            where TCommand : class, new();
        
        Task<string> PublishCommandAsync(
            string commandName,
            object command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null,
            bool typeHandler = false);


        Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommand, TCommandResult>(
            
            TCommand command,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null
        )
            where TCommand : class, IHaveResult<TCommandResult>, new()
            where TCommandResult : class, ICommandResult, new();
        
        Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommandResult>(
            string commandName,
            object command,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            bool throwIfTimeout = true,
            string handlerServiceType = null,
            string handlerServiceName = null
        ) where TCommandResult : class, new();


        Task PublishResultAsync(ICommandResult result, CommandContext commandContext);
        Task PublishResultAsync(CommonCommandResult result, CommandContext commandContext);

        Task PublishResultAsync(object result, string resultCode, CommandContext commandContext);

        Task PublishResultAsync(InternalExceptionDTO exceptionDTO, CommandContext commandContext);
        Task PublishResultAsync(Exception exception, string errorCode, CommandContext commandContext);

        Task PublishResultAsync(IList<FieldError> validationErrors, CommandContext commandContext);

        Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandContext commandContext)
            where TCommandResult : class, ICommandResult, new();

        
        Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null);
        Task PublishCEventAsync(IEvent evnt, string handlerServiceType, TimeSpan? ttl = null);
        
        Task PublishEventAsync(string eventName, object evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null);

        Task PublishCEventAsync(string eventName, object evnt, string handlerServiceType, TimeSpan? ttl = null);

        Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDTO);
        Task RaiseExceptionDetectEvent(string cid, Exception ex);
    }
}