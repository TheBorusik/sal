using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public class SalServiceTypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public SalServiceTypeAttribute(string type)
        {
            Type = type;
        }
    }
}