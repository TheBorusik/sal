using System;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class SalCommandNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public SalCommandNameAttribute(string name)
        {
            Name = name;
        }
    }
}