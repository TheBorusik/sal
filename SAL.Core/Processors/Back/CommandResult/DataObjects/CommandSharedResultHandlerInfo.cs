using System;
using Newtonsoft.Json.Schema;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Processors
{
    internal class CommandSharedResultHandlerInfo
    {
        public Type HandlerType;
        public ISubscription privateSubscription;
        public ISubscription sharedSubscription;

        public CommonSharedCommandResultConfig config;
    }
}