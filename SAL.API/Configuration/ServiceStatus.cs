using System;

namespace SAL.API
{
    public static class ServiceStatus
    {
        public static DateTime StarTime { get; internal set; }
        public static bool IsOnline { get; internal set; }
        public static bool ShutDownStateOn { get; internal set; }
        public static string ContourHost { get; internal set; }
    }
}