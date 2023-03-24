using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
	public static class CardNumberHelper
    {
        public static char MaskChar = '*';

        public static string MaskNumber(this string number, int partLength = 0)
        {
            if (string.IsNullOrWhiteSpace(number))
                return number;

            var noSpace = Regex.Replace(number, @"\s+", "");

            if (!Regex.IsMatch(noSpace, @"^\d{13,19}$"))
                return number;

            var mnumber = Regex.Replace(noSpace, @"(\d{6})(\d{3,9})(\d{4})",
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);

            if (partLength < 1)
                return mnumber;

            var parts = mnumber.SplitInParts(partLength);
            return string.Join(" ", parts);
        }

        public static string MaskNumber(this JValue value)
        {
            return MaskNumber(value.Value != null ? value.Value.ToString() : "");
        }

        public static string Repeat(char ch, int length)
            => new StringBuilder().Append(ch, length).ToString();

        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength < 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));
            if (partLength == 0)
            {
                yield return s;
                yield break;
            }

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }
    }
}