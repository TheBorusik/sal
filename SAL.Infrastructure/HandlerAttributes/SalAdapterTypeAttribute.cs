using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SalAdapterTypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public SalAdapterTypeAttribute(string type)
        {
            Type = type;
        }
    }
}