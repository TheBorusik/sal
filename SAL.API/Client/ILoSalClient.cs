using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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


        Task PublishResultAsync(
            CommonCommandResult result, 
            CommandDescriptor commandDescriptor);

        Task PublishEventAsync(
            string eventName, 
            object eventBody, 
            TimeSpan? ttl,
            string handlerServiceType,
            string handlerServiceName);
        
        Task PublishCommandAsync(CommandDescriptor commandDescriptor, JObject commandBody);

        Task PublishEventAsync(EventDescriptor eventDescriptor, JObject eventBody);
    }
}