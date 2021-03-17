

using System.Threading;

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
        private static readonly AsyncLocal<string> handlerName = new();
        private static readonly AsyncLocal<string> handlerType = new();
        
        public static string Name
        {
            get
            {
                var name  = handlerName.Value;
                return string.IsNullOrWhiteSpace(name) ? "sal" : name;
            }
            internal set => handlerName.Value = value;
        }

        public static string Type
        {
            get
            {
                var name = handlerType.Value;
                return string.IsNullOrWhiteSpace(name) ? "unknown" : name;
            }
            internal set => handlerType.Value = value;
        }
        
    }
}