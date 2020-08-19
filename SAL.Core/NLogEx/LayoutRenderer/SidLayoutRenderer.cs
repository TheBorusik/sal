using System.Text;
using NLog;
using NLog.LayoutRenderers;
using SAL.API;

namespace SAL.Core.NLogEx.LayoutRenderer
{
    [LayoutRenderer("sid")]
    public class SidLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (SessionManager.Current != null)
            {
                var sessionId = SessionManager.Current.GetSafeValue(SessionNames.SessionId, "");
                var operationId = SessionManager.Current.GetSafeValue(SessionNames.OperationId, "");


                if (!string.IsNullOrEmpty(sessionId))
                    builder.Append($"[SID:{sessionId}:{operationId}]");
            }
        }
    }
}
