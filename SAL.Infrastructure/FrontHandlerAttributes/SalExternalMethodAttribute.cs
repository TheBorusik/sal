using System;

namespace SAL.Infrastructure
{
    [Obsolete("Use SalRequestTypeAttribute for Command or CommonCommandHandler or ResultHandler")]
    [AttributeUsage(AttributeTargets.Class)]
    public class SalExternalMethodAttribute : Attribute
    {
        public string ServiceMethod { get; private set; }

        public SalExternalMethodAttribute(string serviceMethod)
        {
            ServiceMethod = serviceMethod;
        }
    }
}