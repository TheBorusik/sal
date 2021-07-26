using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true,Inherited = false)]
    public class SalRequestTypeAttribute : Attribute
    {
        public string RequestType { get; private set; }

        public SalRequestTypeAttribute(string requestType)
        {
            RequestType = requestType;
        }
    }
}