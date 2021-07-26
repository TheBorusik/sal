using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SAL.Infrastructure;

namespace SAL.API
{
    public abstract class FrontBaseExecuteHandler<TExternalCommand, TExternalCommandResult, TInternalCommand, TInternalCommandResult> 
        : IFrontCommandHandler2Async<TExternalCommand,TExternalCommandResult>
        where TInternalCommand : class, IHaveResult<TInternalCommandResult>, new()
        where TInternalCommandResult : class, ICommandResult, new()
    {
        protected readonly ISalClient backClient;
        protected CommandContext commandContext;
        protected ExecutingContext executingContext;
        
        protected FrontBaseExecuteHandler(ISalClient backClient)
        {
            this.backClient = backClient;
        }

        protected abstract Task<TInternalCommand> Transform(TExternalCommand command);

        protected virtual Task<CommonCommandResult> Transform(CommandResult<TInternalCommandResult> result)
        {
            return Task.FromResult(result.ToCommon());
        }

        protected virtual async Task<bool> ProcessingError(Exception ex)
        {
            executingContext.Logger.LogError(ex, $"Внутренняя ошибка");
            await executingContext.SalClient.PublishResultAsync(ex.ToDto(SalErrorCodes.InternalError), commandContext);
            return true;
        }
        

        
        public async Task Handle(TExternalCommand command, CommandContext commandContext, ExecutingContext executingContext)
        {
            try
            {
                var internalCommand = await Transform(command);

                var internalResult = await backClient.ExecuteCommandAsync<TInternalCommand, TInternalCommandResult>(internalCommand, commandContext.Descriptor.Priority, commandContext.Descriptor.TTL);

                var externalResult = await Transform(internalResult);

                await executingContext.SalClient.PublishResultAsync(externalResult, commandContext);
            }
            catch (Exception ex)
            {
                if (!await ProcessingError(ex))
                    throw;
            }
        }
    }
}