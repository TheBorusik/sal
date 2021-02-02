using System;
using System.Text;

namespace SAL.API
{
    public static class SalEncoding
    {
        private static Encoding encoding = new UTF8Encoding(false);

        public static byte[] GetBytes(string s)
        {
            return encoding.GetBytes(s);
        }

        /*public static string GetString(ReadOnlySpan<byte> bytes)
        {
            return encoding.GetString(bytes);
        }*/
        
        public static string GetString(byte[] bytes)
        {
            return encoding.GetString(bytes);
        }

        public static string GetString(byte[] bytes, int index, int count)
        {
            return encoding.GetString(bytes, index, count);
        }

    }
}