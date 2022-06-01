using System;
using System.Threading.Tasks;

namespace SAL.API
{
    public interface ILoSalClient
    {

        (string, string) GetRouteForCommonSharedResult();
        
        Task LoPublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId,
            CommandPriority priority,
            TimeSpan? ttl,
            string commandExchangeName,
            string commandRoutingKey,
            string resultExchangeName,
            string resultRoutingKey);

        Task<SimpleCommandResult> LoExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            string commandExchangeName,
            string commandRoutingKey,
            bool throwIfTimeout
        );

        Task LoPublishEventAsync(
            string eventName,
            object eventBody,
            string correlationId,
            TimeSpan? ttl,
            string exchangeName,
            string routingKey);
        
        Task<SimpleCommandResult> LoExecuteAsync(CommandContext commandContext, object commandBody, bool throwIfTimeout);
        Task LoPublishResultAsync(CommandContext commandContext, CommonCommandResult result);
        Task LoPublishAsync(CommandContext commandContext, object commandBody);
        Task LoPublishAsync(CommandResultContext commandResultContext, CommonCommandResult result);
        Task LoPublishAsync(EventContext eventContext, object eventBody);
    }
    
}