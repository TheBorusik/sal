namespace SAL.API
{
    public static class HumanReadableDataSizeHelper
    {
        public static string HumanReadable(this int value)
        {
            return HumanReadable((long)value);
        }


        public static string HumanReadable(this long value)
        {
            decimal len = value;
            var sizes = new[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024M;
            }
            return $"{len:0.##} {sizes[order]}";
        }



    }
}