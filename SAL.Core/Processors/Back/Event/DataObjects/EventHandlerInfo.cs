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
        public MethodInfo HandleMethod;
        public bool IsCommon;

    }
}