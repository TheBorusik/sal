using System;
using Microsoft.Extensions.Logging;

namespace SAL.API
{
    public static class eeLoggerHelper
    {

        public static void Error(this ILogger logger, InternalExceptionDTO dto)
        {
            logger.Log(LogLevel.Error, dto.Message);
        }

        public static void Error(this ILogger logger, InternalExceptionDTO dto, Exception ex)
        {
            logger.Log(LogLevel.Error, ex, dto.Message);
        }

        public static void Error(this ILogger logger, string msg, Exception ex)
        {
            logger.Log(LogLevel.Error, ex, msg);
        }


        public static void Critical(this ILogger logger, InternalExceptionDTO dto)
        {
            logger.Log(LogLevel.Critical, dto.Message);
        }

        public static void Critical(this ILogger logger, InternalExceptionDTO dto, Exception ex)
        {
            logger.Log(LogLevel.Critical, ex, dto.Message);
        }

        public static void Critical(this ILogger logger, string msg, Exception ex)
        {
            logger.Log(LogLevel.Critical, ex, msg);
        }


        public static void Fatal(this ILogger logger, InternalExceptionDTO dto)
        {
            logger.Log(LogLevel.Critical, dto.Message);
        }

        public static void Fatal(this ILogger logger, InternalExceptionDTO dto, Exception ex)
        {
            logger.Log(LogLevel.Critical, ex, dto.Message);
        }

        public static void Fatal(this ILogger logger, string msg, Exception ex)
        {
            logger.Log(LogLevel.Critical, ex, msg);
        }


        public static void Trace(this ILogger logger, string msg)
        {
            logger.Log(LogLevel.Trace, msg);
        }
        public static void Debug(this ILogger logger, string msg)
        {
            logger.Log(LogLevel.Debug, msg);
        }

        public static void Info(this ILogger logger, string msg)
        {
            logger.Log(LogLevel.Information, msg);
        }

        public static void Information(this ILogger logger, string msg)
        {
            logger.Log(LogLevel.Information, msg);
        }

        public static void Warning(this ILogger logger, string msg)
        {
            logger.Log(LogLevel.Warning, msg);
        }

        public static void Warning(this ILogger logger,  string msg, Exception ex)
        {
            logger.Log(LogLevel.Warning, ex, msg);
        }

    }
}
