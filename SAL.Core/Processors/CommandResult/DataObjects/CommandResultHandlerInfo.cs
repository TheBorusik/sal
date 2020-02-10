using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SAL.API;

namespace SAL.Core.Processors
{


    internal class CommandResultHandlerInfo
    {
        public string CommandName;
        public Type CommandType;
        public Type ResultType;

        public Type HandlerType;

        public MethodInfo HandlerMethod;
        public bool IsCommon;
    }

    internal class CommandResultProcessorConfig
    {
        public ushort GlobalPrefetchCount { get; set; } = 25;
        public ushort InstancePrefetchCount { get; set; } = 15;
        public ushort TypePrefetchCount { get; set; } = 5;
        public ushort SyncPrefetchCount { get; set; } = 5;

    }




    internal class SimpleCommandResultHandlerInfo
    {
        public string CommandCorrelationId;
        public DateTime ExpireDate;
        public TaskCompletionSource<CommonCommandResult> CompletionSource;
        public CancellationTokenSource CancellationTokenSource;

        //   public Canselation

    }



}