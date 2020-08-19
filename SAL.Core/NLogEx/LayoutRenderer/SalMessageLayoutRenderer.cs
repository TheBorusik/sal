using System.Text;
using System.Text.RegularExpressions;
using NLog;
using NLog.LayoutRenderers;

namespace SAL.Core.NLogEx.LayoutRenderer
{
    [LayoutRenderer("message")]
    public class SalMessageLayoutRenderer : global::NLog.LayoutRenderers.MessageLayoutRenderer
    {
        //  private const string PanPattern1 = "((cardnumber|pan)\"\\s?:\\s?\"\\d{6})(\\d{6,9}?)(\\d{4}\")";
        private const string PanPattern2 = "(\"\\d{6})(\\d{6,9}?)(\\d{4}\")";
        private const string CvvPattern = "(cvv\"\\s?:\\s?\")(\\d{3,})(\")";
        private const string PassPattern = "((password|pwd)\"\\s?:\\s?\")(.*?)(\")";
        private const char MaskChar = '*';

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var messageBuilder = new StringBuilder();
            base.Append(messageBuilder, logEvent);
            var strMsg = messageBuilder.ToString();


            //       strMsg = Regex.Replace(strMsg, PanPattern1, 
            //           m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[3].Value.Length) + m.Groups[4].Value, RegexOptions.IgnoreCase);
            strMsg = Regex.Replace(strMsg, PanPattern2,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);
            strMsg = Regex.Replace(strMsg, PassPattern,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[3].Value.Length) + m.Groups[4].Value, RegexOptions.IgnoreCase);
            strMsg = Regex.Replace(strMsg, CvvPattern,
                m => m.Groups[1].Value + Repeat(MaskChar, m.Groups[2].Value.Length) + m.Groups[3].Value, RegexOptions.IgnoreCase);

            builder.Append(strMsg);
        }


        private string Repeat(char ch, int length)
            => new StringBuilder().Append(ch, length).ToString();
    }
}