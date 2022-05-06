using System;
using System.Collections.Generic;
using System.Reflection;

namespace SAL.Core.Processors
{
    internal class EventInfo
    {
        public string EventName;
        public bool Preserved;
        public bool OneInstance;
        public List<EventHandlerInfo> Handlers = new ();
    }
    
    
    internal class EventHandlerInfo
    {
        public Type HandlerType;

        public Type EventType;
        public MethodInfo HandleMethod;

        public bool IsCommon;

    }
}