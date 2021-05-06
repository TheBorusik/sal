

using System.Threading;

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
    }

    public class HandlerData
    {
        public HandlerTypes HandlerType;
        public string HandlerName;
        public string CorrelationId;
        
        public string SessionId;

        public long? ProcessId;
        public long? AuthId;

    }

    public static class HandlerContext
    {
        private static readonly AsyncLocal<HandlerData> data = new();

        public static void Set(HandlerTypes handlerType, string handlerName, string correlationId = "")
        {
            data.Value = new HandlerData
            {
                CorrelationId = correlationId,
                HandlerType = handlerType,
                HandlerName = handlerName
            };
        }

        public static void UpdateCorrelationId(string correlationId)
        {
            if(data.Value == null)
                return;
            data.Value.CorrelationId = correlationId;
        }
        
        public static void Update(TransportMessage msg)
        {
            if(data.Value == null)
                return;
            if(msg.SessionInfo == null)
                return;

            data.Value.SessionId = msg.SessionInfo.SessionId;
            data.Value.AuthId = msg.SessionInfo.AuthId;
            data.Value.ProcessId = msg.SessionInfo.ProcessId;

        }
        
        public static void Update(HandlerTypes handlerType = HandlerTypes.Unknown , string handlerName = "")
        {
            if(data.Value == null)
                return;
            if (handlerType != HandlerTypes.Unknown)
                data.Value.HandlerType = handlerType;
            if (!string.IsNullOrWhiteSpace(handlerName))
                data.Value.HandlerName = handlerName;
        }
        
        
        public static string HandlerName => data.Value.HandlerName;
        public static HandlerTypes HandlerType => data.Value.HandlerType;
        public static string SessionId => data.Value.SessionId;
        public static string CorrelationId => data.Value.CorrelationId;
        public static long? ProcessId => data.Value.ProcessId;
        public static long? AuthId => data.Value.AuthId;
        
    }
}