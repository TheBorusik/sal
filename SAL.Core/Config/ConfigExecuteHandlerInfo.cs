using System.Threading;
using System.Threading.Tasks;
using SAL.Core.Config.Messages;

namespace SAL.Core.Config
{
    public class ConfigExecuteHandlerInfo
    {
        public TaskCompletionSource<ConfigMessage>  CompletionSource;
        public CancellationTokenSource CancellationTokenSource;
    }
}