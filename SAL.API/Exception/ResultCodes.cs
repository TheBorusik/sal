using System;

namespace SAL.API
{
    public static partial class ResultCodes
    {
        public const string Error = "Error";
        public const string Success = "Success";
    }
    
    public static partial class SalErrorCodes
    {
        public const string Fatal = "Fatal";
        public const string ValidationFailed = "ValidationFailed";
        public const string NotHandledCommandResult = " NotHandledCommandResult";
        public const string NotImplemented = " NotImplemented";
        public const string NotHandledCommand = "NotHandledCommand";
        public const string NotHandledEvent = "NotHandledEvent";
        public const string InternalError = "InternalError";
        public const string UnknownResultCode = "UnknownResultCode";
        public const string NotFound = "NotFound";
        public const string Obsolete = "Obsolete";
    }
}