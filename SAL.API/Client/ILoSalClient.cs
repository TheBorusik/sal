using System;
using System.Threading.Tasks;

namespace SAL.API.Client
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
            int ttls,
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
            bool isSystem,
            string handlerServiceType,
            string handlerServiceName);
    }
}