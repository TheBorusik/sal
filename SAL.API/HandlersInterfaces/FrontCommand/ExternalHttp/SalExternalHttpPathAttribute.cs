using System;

namespace SAL.API
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SalExternalHttpPathAttribute : Attribute
    {
        public string Uri { get; private set; }

        public SalExternalHttpPathAttribute(string uri)
        {
            Uri = uri;
        }
    }
}