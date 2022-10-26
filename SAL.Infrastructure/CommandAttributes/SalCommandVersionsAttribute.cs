using System;
using System.Linq;

namespace SAL.Infrastructure
{
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Method)]
    public class SalCommandVersionsAttribute : Attribute
    {
        public string[] Versions { get; }

        public SalCommandVersionsAttribute(params string[] versions)
        {
            Versions = versions.ToArray();
        }
    }
}