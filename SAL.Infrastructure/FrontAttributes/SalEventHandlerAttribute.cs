using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SalExternalMethodAttribute : Attribute
    {
        public string ServiceMethod { get; private set; }

        public SalExternalMethodAttribute(string serviceMethod)
        {
            ServiceMethod = serviceMethod;
        }
    }


    [AttributeUsage(AttributeTargets.Class , AllowMultiple = true)]
    public class SalExternalUriAttribute : Attribute
    {
        public string Uri { get; private set; }

        public SalExternalUriAttribute(string uri)
        {
            Uri = uri;
        }
    }


}