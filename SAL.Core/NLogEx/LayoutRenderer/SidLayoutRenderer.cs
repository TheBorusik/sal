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
            var session = SessionManager.Current;

            if (session != null)
            {
                var sessionId = session.GetSafeValue(SessionNames.SessionId, "");
                var operationId = session.GetSafeValue(SessionNames.OperationId, "");
                var oList = session.GetSafeValue(SessionNames.OperationList, new long[0]);

                builder.Append($"[SID:{sessionId}:{string.Join(":", oList)}:{operationId}]");
            }
        }
    }
}