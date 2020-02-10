using System;

namespace SAL.Infrastructure.EventAttributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SalEventHandlerAttribute : Attribute
    {
        public string EventName { get; private set; }

        public SalEventHandlerAttribute(string name)
        {
            EventName = name;
        }
    }
}