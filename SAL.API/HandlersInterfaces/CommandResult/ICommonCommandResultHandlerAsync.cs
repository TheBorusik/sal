using System.Threading.Tasks;

namespace SAL.API.CommandResult
{
    public interface ICommonCommandResultHandler : ICommandResultHandler
    {
        Task<bool> ResultHandle(CommonCommandResult result);
    }
}