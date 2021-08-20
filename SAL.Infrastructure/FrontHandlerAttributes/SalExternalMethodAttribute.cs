using System;

namespace SAL.Infrastructure
{
    [Obsolete("Use SalCommandNameAttribute or BackCommandNameAttribute for BaseBackCommandHandler or FrontCommandNameAttribute for BaseFrontCommandHandler ")]
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