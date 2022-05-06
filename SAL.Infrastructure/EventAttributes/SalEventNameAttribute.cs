using System;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class SalEventNameAttribute : Attribute
    {
        public string Name { get; }
        public bool Preserved { get;  }
        public bool OneInstance { get;  }

        public SalEventNameAttribute(string name, bool preserved = true, bool oneInstance = false)
        {
            Name = name;
            Preserved = preserved;
            OneInstance = oneInstance;
        }
    }
}