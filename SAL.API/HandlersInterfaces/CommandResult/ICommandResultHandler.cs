using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.CommandResult
{

    public interface ICommandResultHandler
    {

    }

    public interface ICommandResultHandlerAsync<TCommand, TCommandResult> : ICommandResultHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task<bool> ResultHandle(CommandResult<TCommandResult> result, CommandResultDescriptor context, ExecutingContext executingContext);
    }
}