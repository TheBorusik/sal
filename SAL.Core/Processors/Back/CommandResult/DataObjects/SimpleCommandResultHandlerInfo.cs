using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAL.Core.Processors
{
    internal class SimpleCommandResultHandlerInfo
    {
        public string CommandCorrelationId;
        public DateTime ExpireDate;
        public TaskCompletionSource<SimpleCommandResult> CompletionSource;
        public CancellationTokenSource CancellationTokenSource;

        //   public Canselation

    }
}