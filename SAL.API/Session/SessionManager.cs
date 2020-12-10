using System;
using System.Linq;
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

        private static void SetSession(JObject session)
        {
            CallContext.SetData(sessionName, session);
        }

        private static JObject CreateNewSession(string sessionId = null)
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

        public static void StartAdapterSession(JObject session)
        {
            var operation = session.GetSafeValue(SessionNames.OperationId, 0L);
            session.AddOrUpdate(SessionNames.OperationId, ++operation);
            SetSession(session);
        }

        public static void IncOperationId()
        {
            var session = Current;
            var operationId = session.GetSafeValue(SessionNames.OperationId, 0L);
            session.AddOrUpdate(SessionNames.OperationId, ++operationId);
        }

        public static void Restore(JObject session)
        {
            if (session == null)
                return;
            
            SetSession(session);
        }

        public static void UpdateCurrent(JObject session)
        {
            if(session ==null)
                return;

            var curSession = Current;
            curSession.RemoveAll();
            session.ForEach(t =>
            {
                try
                {
                    if (t is JProperty p)
                    {
                        curSession.AddOrUpdate(p.Name, p.Value.DeepClone());
                    }
                }
                catch (Exception)
                {
                    //
                }

            });

        }

        public static void UpdateCurrent(JObject session, Func<string, bool> checkKey)
        {
            if (session == null)
                return;

            var curSession = Current;
            curSession.RemoveAll();
            session.ForEach(t =>
            {
                if (!(t is JProperty p)) return;

                if (!checkKey(p.Name))
                    return;
                
                try
                {
                    curSession.AddOrUpdate(p.Name, p.Value.DeepClone());
                }
                catch (Exception)
                {
                    //
                }
            });

        }

        public static void Merge(JObject session)
        {
            if (session == null)
                return;

            var curSession = Current;
            if (curSession == null)
                return;


            var notMergedProp = new string[] {SessionNames.OperationId, SessionNames.SessionId, SessionNames.Version};


            session.ForEach(t =>
            {
                if (!(t is JProperty p)) return;

                if (notMergedProp.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase))
                    return;

                curSession.AddOrUpdate(p.Name, p.Value);
            });
        }


        public static void Merge(JObject session, Func<string, bool> checkKey)
        {
            if (session == null)
                return;

            var curSession = Current;
            if (curSession == null)
                return;

            session.ForEach(t =>
            {
                if (!(t is JProperty p)) return;

                if (!checkKey(p.Name))
                    return;

                curSession.AddOrUpdate(p.Name, p.Value);
            });
        }

    }
}