using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;


namespace SAL.API
{
    public static class SessionManager
    {
        private static readonly AsyncLocal<JObject> context = new();

        public static JObject Current
        {
            get
            {
                var session = context.Value;
                if (session == null)
                {
                    context.Value = session = SetNewSession();
                }

                return session;
            }
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
            context.Value = session;
            return session;
        }

        public static void StartAdapterSession(JObject session)
        {
            var operation = session.GetSafeValue(SessionNames.OperationId, 0L);
            session.AddOrUpdate(SessionNames.OperationId, ++operation);

            context.Value = session;
        }

        public static void IncOperationId()
        {
            var session = Current;
            var operationId = session.GetSafeValue(SessionNames.OperationId, 0L);
            session.AddOrUpdate(SessionNames.OperationId, ++operationId);
        }

        public static void Set(JObject session)
        {
            if(context.Value == null)
                context.Value = session.Clone();
            else
            {
                context.Value.Merge(session, new JsonMergeSettings
                {
                    MergeNullValueHandling = MergeNullValueHandling.Merge,
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase
                });
            }
        }
    }
}