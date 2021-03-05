using System;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class)]
    public class SalEventNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public SalEventNameAttribute(string name)
        {
            Name = name;
        }
    }
}