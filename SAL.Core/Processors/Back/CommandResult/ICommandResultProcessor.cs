using System.Threading.Tasks;
using SAL.API;

namespace SAL.Core.Processors
{
    public interface ICommandResultProcessor
    {
        void RegisterSimpleCommandResultHandler(string correlationId, TaskCompletionSource<CommonCommandResult> completionSource, int timeOut);
    }
}