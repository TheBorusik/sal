using System;

namespace SAL.API
{
    public class SizeLimitException : Exception
    {
        public SizeLimitException()
            : base("File is too large")
        {
        }

        public SizeLimitException(long sizeLimit)
            : base($"File is too large. Size limit is {sizeLimit} bytes.")
        {
        }
    }
}
