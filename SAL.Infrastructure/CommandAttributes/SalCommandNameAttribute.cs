using System;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class)]
    public class SalCommandNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public SalCommandNameAttribute(string name)
        {
            Name = name;
        }
    }
}