using System;

namespace SAL.API
{
    public class SalCommandTimeoutException : Exception
    {
        public SalCommandTimeoutException() : base("Command Timeout")
        {
        }
    }
    
    public class SalNotConfiguredException : Exception
    {
        public SalNotConfiguredException(string configurationName) : base($"Configuration not exist ({configurationName})")
        {
        }
    }
    
    public class SalUnknownContourException : Exception
    {
        public SalUnknownContourException() : base("Unknown Contour")
        {
        }
    }

    public class ContourNotSupportedException : Exception
    {
        public ContourNotSupportedException() : base("Contour Not Supported for this method")
        {
        }
    }
}