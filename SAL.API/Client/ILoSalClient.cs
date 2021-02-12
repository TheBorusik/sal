using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API.FrontCommand;

namespace SAL.API
{
    public interface ILoSalClient
    {
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

        Task<CommonCommandResult> ExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName
        );
        
        Task<CommonCommandResult> ExecuteExternalHttp(
            ExternalHttpRequest request,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName);


        Task PublishResultAsync(
            CommonCommandResult result, 
            CommandDescriptor commandDescriptor);

        Task PublishEventAsync(
            string eventName, 
            object eventBody, 
            TimeSpan? ttl,
            bool isSystem,
            string handlerServiceType,
            string handlerServiceName);
        
        Task PublishEventAsync(
            string eventName, 
            object eventBody,
            string correlationId,
            TimeSpan? ttl,
            bool isSystem,
            string handlerServiceType,
            string handlerServiceName);
        
        Task PublishCommandAsync(CommandDescriptor commandDescriptor, JObject commandBody);

        Task PublishResultAsync(CommandResultDescriptor commandResultDescriptor, CommonCommandResult result);

        Task PublishEventAsync(EventDescriptor eventDescriptor, JObject eventBody);
    }
    
}