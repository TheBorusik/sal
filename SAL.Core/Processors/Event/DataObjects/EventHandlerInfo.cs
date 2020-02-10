using System;
using System.Reflection;

namespace SAL.Core.Processors
{
    internal class EventHandlerInfo
    {
        public Type HandlerType;
        public string EventName;
        public Type EventType;
        public bool IsSystem;
        public MethodInfo HandlerMethod;
        public bool IsCommon;

    }


    internal class EventProcessorConfig
    {
        public ushort PrefetchCount { get; set; } = 25;
        public ushort SystemPrefetchCount { get; set; } = 15;
    }

}