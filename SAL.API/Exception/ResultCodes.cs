namespace SAL.API
{
    public static partial class ResultCodes
    {
        public const string Error = "Error";
        public const string Success = "Success";
        public const string SalCommandTimeout = "SalCommandTimeout";
    }
    
    public static partial class SalErrorCodes
    {
        public const string Fatal = "Fatal";
        public const string ValidationFailed = "ValidationFailed";
        public const string InternalError = "InternalError";
        public const string ExternalError = "ExternalError";
        
        public const string NotHandledCommandResult = " NotHandledCommandResult";
        public const string NotFound = "NotFound";
        public const string NotSuccess = "NotSuccess";
        
        

        
        public const string NotImplemented = " NotImplemented";
        public const string NotHandledCommand = "NotHandledCommand";
        public const string NotHandledEvent = "NotHandledEvent";
        public const string UnknownResultCode = "UnknownResultCode";
        public const string Obsolete = "Obsolete";
        public const string NotError = "NotError";
    }


    public static partial class SalErrorMessages
    {
        public const string RedisNotConfigured = "Redis Not Configured";
        public const string SessionIdNotSet = "SessionId Not Set";
        public const string SessionNotFound = "Session Not Found";
    }
}