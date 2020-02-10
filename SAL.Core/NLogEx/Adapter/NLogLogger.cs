namespace SAL.Core.NLogEx.Adapter
{

    /*
    internal class NLogLogger : AbstractLogger
    {
        private static readonly Type declaringType = typeof(NLogLogger);
        private readonly Logger logger;


        protected internal NLogLogger(Logger logger)
        {
            this.logger = logger;
        }

        public override bool IsTraceEnabled => logger.IsTraceEnabled;
        public override bool IsDebugEnabled => logger.IsDebugEnabled;
        public override bool IsErrorEnabled => logger.IsErrorEnabled;
        public override bool IsFatalEnabled => logger.IsFatalEnabled;
        public override bool IsInfoEnabled => logger.IsInfoEnabled;
        public override bool IsWarnEnabled => logger.IsWarnEnabled;

        protected override void WriteInternal(LogLevel level, object message, Exception exception)
        {
            var logEvent = new LogEventInfo(GetLevel(level), logger.Name, null, "{0}", new object[1]
            {
                message
            }, exception);

            logger.Log(declaringType, logEvent);
        }

        private static NLog.LogLevel GetLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Fatal:
                    return NLog.LogLevel.Fatal;
                case LogLevel.Off:
                    return NLog.LogLevel.Off;
                case LogLevel.All:
                    return NLog.LogLevel.Trace;
                case LogLevel.Trace:
                    return NLog.LogLevel.Trace;
                case LogLevel.Debug:
                    return NLog.LogLevel.Debug;
                case LogLevel.Info:
                    return NLog.LogLevel.Info;
                case LogLevel.Warn:
                    return NLog.LogLevel.Warn;
                case LogLevel.Error:
                    return NLog.LogLevel.Error;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "unknown log level");
            }
        }



        public override void InfoFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            var logEvent = new LogEventInfo(NLog.LogLevel.Info, logger.Name, null, format, args);
            logger.Log(declaringType, logEvent);
        }

        public override void InfoFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args)
        {

            var logEvent = new LogEventInfo(NLog.LogLevel.Info, logger.Name, formatProvider, format, args, exception);
            logger.Log(declaringType, logEvent);
        }

        public override void InfoFormat(string format, params object[] args)
        {
            var logEvent = new LogEventInfo(NLog.LogLevel.Info, logger.Name, null, format, args);
            logger.Log(declaringType, logEvent);
        }

        public override void InfoFormat(string format, Exception exception, params object[] args)
        {
            var logEvent = new LogEventInfo(NLog.LogLevel.Info, logger.Name, null, format, args, exception);
            logger.Log(declaringType, logEvent);
        }
    }
    */
}