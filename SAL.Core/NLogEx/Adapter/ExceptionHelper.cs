using System;
using System.Threading;
using NLog;
using NLog.Common;

namespace SAL.Core.NLogEx.Adapter
{
    internal static class ExceptionHelper
    {
        private const string LoggedKey = "NLog.ExceptionLoggedToInternalLogger";

        public static void MarkAsLoggedToInternalLogger(this Exception exception)
        {
            if (exception == null)
                return;
            exception.Data[(object)"NLog.ExceptionLoggedToInternalLogger"] = (object)true;
        }

        public static bool IsLoggedToInternalLogger(this Exception exception)
        {
            if (exception != null)
                return exception.Data[(object)"NLog.ExceptionLoggedToInternalLogger"] as bool? ?? false;
            return false;
        }

        public static bool MustBeRethrown(this Exception exception)
        {
            if (exception.MustBeRethrownImmediately())
                return true;
            bool flag = exception is NLogConfigurationException;
            if (!exception.IsLoggedToInternalLogger())
            {
                NLog.LogLevel level = flag ? NLog.LogLevel.Warn : NLog.LogLevel.Error;
                InternalLogger.Log(exception, level, "Error has been raised.");
            }
            int num;
            if (!flag)
            {
                num = LogManager.ThrowExceptions ? 1 : 0;
            }
            else
            {
                bool? configExceptions = LogManager.ThrowConfigExceptions;
                num = configExceptions.HasValue ? (configExceptions.GetValueOrDefault() ? 1 : 0) : (LogManager.ThrowExceptions ? 1 : 0);
            }
            return num != 0;
        }

        public static bool MustBeRethrownImmediately(this Exception exception)
        {
            return exception is StackOverflowException || exception is ThreadAbortException || exception is OutOfMemoryException;
        }



    }
}