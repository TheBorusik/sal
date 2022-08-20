using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
            string resultAdapterName = null,
            JObject meta = null);
        
        Task PublishCommandFafAsync(
            string commandName,
            object commandBody,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null);

        Task<string> PublishCommandWithSharedResultHandlerAsync(
            string commandName,
            object commandBody,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null, 
            string handlerAdapterName = null,
            JObject meta = null);
        
        
        Task<SimpleCommandResult> ExecuteCommandAsync(            
            string commandName,
            object commandBody,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            bool throwIfTimeout = true);
        
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
            TimeSpan? ttl = null, 
            string handlerAdapterType = null, 
            string handlerAdapterName = null);
        
        Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDto);
        

    }
}