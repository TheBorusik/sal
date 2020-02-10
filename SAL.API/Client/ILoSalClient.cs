using System;
using System.Threading.Tasks;

namespace SAL.API.Client
{
    public interface ILoSalClient
    {
        Task PublishCommandAsync(
            string handlerServiceType,
            string commandName, 
            object commandBody, 
            string correlationId, 
            CommandPriority priority,
            TimeSpan? ttl,
            string handlerServiceName,
            string resultServiceType,
            string resultServiceName);

        Task<CommonCommandResult> ExecuteCommandAsync(
            string handlerServiceType,
            string commandName,
            object commandBody,
            CommandPriority priority,
            int ttls,
            string handlerServiceName
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
    }
}