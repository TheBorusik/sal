using System.Threading;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class SessionManager
    {
        private static string sessionName = "sal#session";
        private static long operationId;

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

        public static JObject CreateNewSession()
        {
            Interlocked.Increment(ref operationId);

            var session = new JObject();
            session.AddOrUpdate(SessionNames.SessionId, $"{ServiceConfiguration.AdapterType}#{ServiceConfiguration.AdapterName}");
            session.AddOrUpdate(SessionNames.Version, 1);
            return session;
        }

        public static JObject SetNewSession()
        {
            var session = CreateNewSession();
            SetSession(session);
            return session;
        }

    }
}