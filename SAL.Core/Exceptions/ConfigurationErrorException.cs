using System;

namespace SAL.Core.Exceptions
{
    class ConfigurationErrorException : Exception
    {
        public ConfigurationErrorException(string message)
            : base(message)
        {
        }
    }


}
