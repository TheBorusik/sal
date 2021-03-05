using System.Threading;
using System.Threading.Tasks;
using SAL.Core.Configuration.Messages;

namespace SAL.Core.Configuration
{
    public class ConfigExecuteHandlerInfo
    {
        public TaskCompletionSource<ConfigMessage>  CompletionSource;
        public CancellationTokenSource CancellationTokenSource;
    }
}