using System;

namespace SAL.Core.SystemEventHandlers
{
    public static class SystemEventTimes
    {
        public static TimeSpan BaseTTL { get; } = TimeSpan.FromSeconds(10); 
    }
}