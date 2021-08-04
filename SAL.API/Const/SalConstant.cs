using System;

namespace SAL.API.Const
{
    public static class SalConst
    {
        public static TimeSpan HearBeatInterval => TimeSpan.FromSeconds(5);
        public static TimeSpan SystemEventTTL => TimeSpan.FromSeconds(10); 
    }
}