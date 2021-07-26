using System.Threading.Tasks;

namespace SAL.API
{
    public interface IWfmResultHandler
    {
        Task Handle(CommonCommandResult processResult, WfmProcessInfo ProcessInfo);
    }
}