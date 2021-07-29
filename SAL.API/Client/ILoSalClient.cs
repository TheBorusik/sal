using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ILoSalClient
    {

        public Task<string> PublishCommandAsync(
            string commandName,
            object commandBody);
        
        Task PublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId,
            CommandPriority priority,
            TimeSpan? ttl,
            string handlerAdapterType,
            string handlerAdapterName,
            string resultAdapterType,
            string resultAdapterName);

        Task<SimpleCommandResult> ExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName
        );
        
        Task<SimpleCommandResult> ExecuteExternalHttp(
            ExternalHttpRequest request,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName);


        Task PublishResultAsync(
            CommonCommandResult result, 
            CommandContext commandContext);
        
        Task PublishEventAsync(
            string eventName, 
            object eventBody,
            string correlationId,
            TimeSpan? ttl,
            string handlerServiceType,
            string handlerServiceName,
            bool isCEvent);
        
        Task PublishCommandAsync(CommandContext commandContext, JObject commandBody);

        Task PublishResultAsync(CommandResultContext commandResultContext, CommonCommandResult result);

        Task PublishEventAsync(EventContext eventContext, JObject eventBody);
    }
    
}