using System.Text;
using System.Text.RegularExpressions;
using NLog;
using NLog.LayoutRenderers;

namespace SAL.Core.NLogEx.LayoutRenderer
{
    [LayoutRenderer("message")]
    public class SalMessageLayoutRenderer : global::NLog.LayoutRenderers.MessageLayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var messageBuilder = new StringBuilder();
            base.Append(messageBuilder, logEvent);
            var strMsg = messageBuilder.ToString();

            strMsg = strMsg.MaskSecretData();
            
            builder.Append(strMsg);
        }
        
    }
}