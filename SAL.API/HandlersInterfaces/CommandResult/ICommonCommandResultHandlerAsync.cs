using System.Threading.Tasks;

namespace SAL.API.CommandResult
{
    public interface ICommonCommandResultHandlerAsync 
    {
        Task<bool> ResultHandle(CommonCommandResult result, CommandResultDescriptor context, ExecutingContext executingContext);
    }
}