using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.Client
{
    public interface ISalClient
    {
        string Contour { get; }
        string ContourName { get; }
        
        // hi level
        Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null,
            string resultServiceType = null,
            string resultServiceName = null
        )
            where TCommand : class, ICommand, new();


        Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommand, TCommandResult>(
            TCommand command,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null
        )
            where TCommand : class, IHaveResult<TCommandResult>, new()
            where TCommandResult : class, ICommandResult, new();


        Task PublishResultAsync(ICommandResult result, CommandDescriptor commandDescriptor);

        Task PublishResultAsync(object result, string code, CommandDescriptor commandDescriptor);

        Task PublishResultAsync(InternalExceptionDTO exceptionDTO, CommandDescriptor commandDescriptor);

        Task PublishResultAsync(IList<FieldError> validationErrors, CommandDescriptor commandDescriptor);

        Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandDescriptor commandDescriptor)
            where TCommandResult : class, ICommandResult, new();

        Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null);


        Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDTO);
        Task RaiseExceptionDetectEvent(string cid, Exception ex);
    }
}