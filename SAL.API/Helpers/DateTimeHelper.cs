using System;

namespace SAL.API
{
    public static class DateTimeHelper
    {
        public static DateTime? ToUtc(this DateTime? value)
        {
            if (!value.HasValue) return null;
            return value.Value.ToUtc();
        }
        public static DateTime ToUtc(this DateTime value)
        {
            switch (value)
            {
                case { Kind: DateTimeKind.Unspecified }:
                    return DateTime.SpecifyKind(value, DateTimeKind.Utc);
                case { Kind: DateTimeKind.Utc }:
                    return value;
                case { Kind: DateTimeKind.Local }:
                    return value.ToUniversalTime();
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        

    }
}