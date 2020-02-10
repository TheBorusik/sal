using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SalCommandResultHandlerAttribute : Attribute
    {
        public string ServiceType { get; private set; }
        public string Name { get; private set; }

        public SalCommandResultHandlerAttribute(string serviceType, string commandName)
        {
            ServiceType = serviceType;
            Name = commandName;
        }
    }
}