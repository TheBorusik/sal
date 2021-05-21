using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using SAL.API;

namespace SAL.Core.NLogEx.LayoutRenderer.HandlerContextRenderer
{
    public class SessionIdLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        [DefaultParameter] 
        public bool Header { get; set; } = true;

        public string Brackets { get; set; } = ""; 

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var sessionId = HandlerContext.SessionId;

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.First();
                    builder.Append(b); 
                }
                
                if(Header)
                    builder.Append($"SID:{sessionId}");
                else
                    builder.Append(sessionId);
                
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.Last();
                    builder.Append(b); 
                }
            }
        }
    }
    
    public class CorrelationIdLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        [DefaultParameter] 
        public bool Header { get; set; } = true;
        public string Brackets { get; set; } = ""; 
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var cid = HandlerContext.CorrelationId;

            if (!string.IsNullOrWhiteSpace(cid))
            {
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.First();
                    builder.Append(b); 
                }
                
                if(Header)
                    builder.Append($"CID:{cid}");
                else
                    builder.Append(cid);
                
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.Last();
                    builder.Append(b); 
                }
            }
            
        }
    }
    
    public class WfmProcessIdLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        [DefaultParameter] 
        public bool Header { get; set; } = true;
        public string Brackets { get; set; } = ""; 
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var pid = HandlerContext.ProcessId;

            if (pid.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.First();
                    builder.Append(b); 
                }
                
                if(Header)
                    builder.Append($"PID:{pid}");
                else
                    builder.Append(pid);
                
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.Last();
                    builder.Append(b); 
                }
            }
        }
    }

    public class AuthIdLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        [DefaultParameter] 
        public bool Header { get; set; } = true;
        public string Brackets { get; set; } = ""; 
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var aid = HandlerContext.AuthId;

            if (aid.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.First();
                    builder.Append(b); 
                }
                
                if(Header)
                    builder.Append($"AID:{aid}");
                else
                    builder.Append(aid);
                
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.Last();
                    builder.Append(b); 
                }
            }
        }
    }

    public class OperationIdLayoutRenderer : global::NLog.LayoutRenderers.LayoutRenderer
    {
        [DefaultParameter] 
        public bool Header { get; set; } = true;
        public string Brackets { get; set; } = ""; 
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var oid = HandlerContext.OperationId;

            if (!string.IsNullOrWhiteSpace(oid))
            {
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.First();
                    builder.Append(b); 
                }
                
                if(Header)
                    builder.Append($"OID:{oid}");
                else
                    builder.Append(oid);
                
                if (!string.IsNullOrWhiteSpace(Brackets))
                {
                    var b = Brackets.Last();
                    builder.Append(b); 
                }
            }
        }
    }

}