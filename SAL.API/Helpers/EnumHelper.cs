using System;

namespace SAL.API
{
    public static class EnumHelper
    {
        public static T ToEnum<T>(this string value, T defaultValue) where T : Enum
        {
            try
            {
                if (value == null) return defaultValue;
                return (T) Enum.Parse(typeof(T), value, true);
            }
            catch //(Exception e)
            {
                return defaultValue;
            }
        }
    }
}