using System;

namespace SAL.Core.SystemHandlers
{
    public static class SystemEventTimes
    {
        public static TimeSpan BaseTTL { get; } = TimeSpan.FromSeconds(10); 
    }
}