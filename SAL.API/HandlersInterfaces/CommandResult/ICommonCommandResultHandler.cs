using System.Threading.Tasks;

namespace SAL.API
{
    public interface ICommonCommandResultHandler : ICommandResultHandler
    {
        Task<bool> ResultHandle(CommonCommandResult result);
    }
}