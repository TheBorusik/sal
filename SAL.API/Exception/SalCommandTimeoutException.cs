using System;

namespace SAL.API
{
    public class SalCommandTimeoutException : Exception
    {

    }
    
    public class SalNotConfiguredException : Exception
    {
        public SalNotConfiguredException(string configurationName) : base($"Configuration not exist ({configurationName})")
        {
        }
    }
}