using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class SessionHelper
    {
        public static string GetSID(this JObject session)
        {
            if (session is null)
                return string.Empty;
            return $"[SID:{session.GetSafeValue(SessionNames.SessionId,"")}:{session.GetSafeValue(SessionNames.OperationId, "")}]";
        }
    }
}
