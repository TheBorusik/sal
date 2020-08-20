

namespace SAL.API
{

    public static class HandlerTypes
    {
        public const string Processor = "Processor";
        public const string CommandHandler = "CommandHandler";
        public const string CommandResultHandler = "CommandResultHandler";
        public const string EventHandler = "EventHandler";
    }

    public static class HandlerContext
    {
        private static string handlerName = "sal#handlername";
        private static string handlerType = "sal#handlertype";
        private static string callTraceName = "sal#calltracename";

        public static string Name
        {
            get
            {
                var name  = CallContext.GetData(handlerName) as string;
                return string.IsNullOrWhiteSpace(name) ? "sal" : name;
            }
            internal set => CallContext.SetData(handlerName, value);
        }

        public static string Type
        {
            get
            {
                var name = CallContext.GetData(handlerType) as string;
                return string.IsNullOrWhiteSpace(name) ? "unknown" : name;
            }
            internal set => CallContext.SetData(handlerType, value);
        }

        private static string CallTrace
        {
            get
            {
                var val = CallContext.GetData(callTraceName) as string;
                return string.IsNullOrWhiteSpace(val) ? AdapterConfiguration.AdapterName : val;
            }
            set => CallContext.SetData(callTraceName, value);
        }



    }
}