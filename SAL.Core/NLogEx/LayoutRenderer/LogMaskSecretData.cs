using System.Text;
using System.Text.RegularExpressions;

namespace SAL.Core.NLogEx.LayoutRenderer
{
    public static class  LogMaskSecretData
    {
        //  private const string PanPattern1 = "((cardnumber|pan)\"\\s?:\\s?\"\\d{6})(\\d{6,9}?)(\\d{4}\")";
        private const string PanPattern2 = "(\"\\d{6})(\\d{3,9}?)(\\d{4}\")";
        private const string CvvPattern = "(cvv\"\\s?:\\s?\")(\\d{3,})(\")";
        private const string CvcPattern = "(cvc\"\\s?:\\s?\")(\\d{3,})(\")";
        private const string PassPattern = "((password|pwd)\"\\s?:\\s?\")(.*?)(\")";
        private const string ApiSecretKey = "((ApiKey|SecretKey|ApiKeyId)\"\\s?:\\s?\")(.*?)(\")";
        private const char MaskChar = '*';

        private static string Repeat(char ch, int length)
            => new StringBuilder().Append(ch, length).ToString();

        public static string MaskSecretData(this string str)
        {
             str = Regex.Replace(str, PanPattern2,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);
             str = Regex.Replace(str, PassPattern,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[3].Value.Length) + m.Groups[4].Value, RegexOptions.IgnoreCase);
             str = Regex.Replace(str, CvvPattern,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);
             str = Regex.Replace(str, CvcPattern,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);
            str = Regex.Replace(str, ApiSecretKey,
                m => m.Groups[1].Value + Repeat(MaskChar, 3) + m.Groups[4].Value, RegexOptions.IgnoreCase);

            return str;
        }
    }
}