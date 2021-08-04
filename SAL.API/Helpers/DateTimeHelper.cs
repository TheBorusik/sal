using System;

namespace SAL.API
{
    public static class DateTimeHelper
    {
        public static DateTime? ToUtc(this DateTime? value)
        {
            if (!value.HasValue) return null;
            return value.Value switch
            {
                { Kind: DateTimeKind.Unspecified } => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
                { Kind: DateTimeKind.Utc } => value,
                { Kind: DateTimeKind.Local } => value.Value.ToUniversalTime(),
                _ => null
            };
        }
        

    }
}