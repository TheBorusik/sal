using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface IWfmResultHandler
    {
        Task Handle(CommonCommandResult processResult, WfmProcessInfo ProcessInfo);
    }
}