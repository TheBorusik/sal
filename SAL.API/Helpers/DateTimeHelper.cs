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
            return value switch
            {
                { Kind: DateTimeKind.Unspecified } => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                { Kind: DateTimeKind.Utc } => value,
                { Kind: DateTimeKind.Local } => value.ToUniversalTime(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
        

    }
}