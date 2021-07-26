using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = true, Inherited = false)]
    public class SalExternalUriAttribute : Attribute
    {
        public string Uri { get; private set; }

        public SalExternalUriAttribute(string uri)
        {
            Uri = uri;
        }
    }
}