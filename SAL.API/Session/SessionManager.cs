using System.Threading;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class SessionManager
    {
        private static string sessionName = "sal#session";

        public static JObject Current
        {
            get
            {
                if (!(CallContext.GetData(sessionName) is JObject session))
                {
                    session = SetNewSession();
                    SetSession(session);
                }

                return session;
            }
        } 

        public static void SetSession(JObject session)
        {
            CallContext.SetData(sessionName, session);
        }

        public static JObject CreateNewSession(string sessionId = null)
        {
            var session = new JObject();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                session.AddOrUpdate(SessionNames.SessionId, sessionId);
            }
            else
            {
                session.AddOrUpdate(SessionNames.SessionId, $"{AdapterConfiguration.AdapterType}#{AdapterConfiguration.AdapterName}");

            }
            session.AddOrUpdate(SessionNames.OperationId, 1L);
            session.AddOrUpdate(SessionNames.Version, 1);
            return session;
        }

        public static JObject SetNewSession(string sessionId = null)
        {
            var session = CreateNewSession(sessionId);
            SetSession(session);
            return session;
        }

        public static JObject StartSession(JObject session)
        {
            var sessionId =
                $"{session.GetSafeValue(SessionNames.SessionId, "")}:{session.GetSafeValue(SessionNames.OperationId, 0L)}";

            session.AddOrUpdate(SessionNames.SessionId, sessionId);
            session.AddOrUpdate(SessionNames.OperationId, 1L);
            return session;
        }

    }
}