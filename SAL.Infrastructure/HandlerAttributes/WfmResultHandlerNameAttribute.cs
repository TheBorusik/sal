using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class WfmResultHandlerNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public WfmResultHandlerNameAttribute(string name)
        {
            Name = name;
        }
    }
}