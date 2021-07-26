using System;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SalEventNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public SalEventNameAttribute(string name)
        {
            Name = name;
        }
    }
}