using System.Text;
using NLog;
using NLog.LayoutRenderers;
using SAL.API;

namespace SAL.Core.NLogEx.LayoutRenderer
{
    [LayoutRenderer("pid")]
    public class PidLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var session = SessionManager.Current;

            if (session != null)
            {
                var processId = session.GetSafeValue(SessionNames.WfmProcessId, "Unknown");
                builder.Append(processId);
            }
            else
            {
                builder.Append("Unknown");
            }
        }
    }
}