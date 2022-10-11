using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public enum HandlerTypes
    {
        Unknown,
        System,
        WatchDog,
        Processor,
        CommandHandler,
        CommandResultHandler,
        EventHandler,
        FrontCommandHandler,
        FrontCommandResultHandler,
        FrontEventHandler,
        FrontExternalHttp,
        WorkflowMachine,
        EmbeddedWfm
    }

    public static class HandlerContext
    {
        private static readonly AsyncLocal<JObject> data = new();

        public static void Set(HandlerTypes handlerType, string handlerName, string correlationId = "")
        {
            data.Value = JObject.FromObject(new
            {
                CorrelationId = correlationId,
                HandlerType = handlerType.ToString(),
                HandlerName = handlerName
            });
        }

        public static void Set(string handlerType, string handlerName, string correlationId = "")
        {
            data.Value = JObject.FromObject(new
            {
                CorrelationId = correlationId,
                HandlerType = handlerType,
                HandlerName = handlerName
            });
        }

        public static void UpdateHandlerType(string handlerType)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("HandlerType", handlerType);
        }

        public static void UpdateHandlerName(string handlerName)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("HandlerName", handlerName);
        }

        public static void UpdateCorrelationId(string correlationId)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("CorrelationId", correlationId);
        }

        public static void UpdateSessionId(string sessionId)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("SessionId", sessionId);
        }

        public static void UpdateAuthId(long authId)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("AuthId", authId);
        }

        public static void UpdateProcessId(long processId)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("ProcessId", processId);
        }

        public static void UpdateOperationId(string operationId)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate("OperationId", operationId);
        }

        public static void Update(JObject contextInfo)
        {
            if (data.Value == null)
                return;
            if (contextInfo == null)
                return;

            data.Value.Merge(contextInfo);
        }

        public static void Update(HandlerTypes handlerType = HandlerTypes.Unknown, string handlerName = "")
        {
            if (data.Value == null)
                return;
            if (handlerType != HandlerTypes.Unknown)
                data.Value.AddOrUpdate("HandlerType", handlerType.ToString());
            if (!string.IsNullOrWhiteSpace(handlerName))
                data.Value.AddOrUpdate("HandlerName", handlerName);
        }

        public static void Update(string handlerType = "Unknown", string handlerName = "")
        {
            if (data.Value == null)
                return;
            if (!string.Equals(handlerType, "Unknown"))
                data.Value.AddOrUpdate("HandlerType", handlerType.ToString());
            if (!string.IsNullOrWhiteSpace(handlerName))
                data.Value.AddOrUpdate("HandlerName", handlerName);
        }


        public static void SetValue(string propertyName, object value)
        {
            if (data.Value == null)
                return;
            data.Value.AddOrUpdate(propertyName, value);
        }

        public static T GetSafeValue<T>(string propertyName, T safeValue)
        {
            try
            {
                if (data.Value == null)
                    return safeValue;
                var token = data.Value.GetValueIC(propertyName);
                return token == null ? safeValue : token.ConvertValue<T>();
            }
            catch
            {
                return safeValue;
            }
        }

        public static JToken GetJValue(string propertyName)
        {
            return data.Value.GetValueIC(propertyName);
        }

        public static T GetValue<T>(string propertyName)
        {
            return data.Value.GetValueIC(propertyName).ConvertValue<T>();
        }

        public static string HandlerName => GetSafeValue("HandlerName", "");
        public static string HandlerType => GetSafeValue("HandlerType", "Unknown");
        public static string SessionId => GetSafeValue("SessionId", "");
        public static string CorrelationId => GetSafeValue("CorrelationId", "");
        public static long? ProcessId => GetSafeValue<long?>("ProcessId", null);
        public static long? AuthId => GetSafeValue<long?>("AuthId", null);
        public static string OperationId => GetSafeValue("OperationId", "");

        public static JObject MakeContextInfo()
        {
            if (data.Value == null)
                return new JObject();

            var res = data.Value.Clone();
            res.Remove("HandlerName");
            res.Remove("HandlerType");
            return res;
        }

        public static JObject GetData()
        {
            if (data.Value == null)
                return new JObject();
            return data.Value.Clone();
        }
    }
}