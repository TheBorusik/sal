namespace SAL.API
{
    public class InternalExceptionDTO : ExceptionDTO
    {
        public string StackTrace { get; set; }
        public string CallTrace { get; set; }
        public string AdapterName { get; set; }
        public string HandlerName { get; set; }
        public string ExceptionType { get; set; }


        public InternalExceptionDTO InnerException { get; set; }

        public InternalExceptionDTO Clone()
        {
            return new InternalExceptionDTO
            {
                Code = Code,
                TimeStamp = TimeStamp,
                Message = Message,
                Properties = Properties.Clone(),
                Sid = Sid,
                StackTrace = StackTrace,
                CallTrace = CallTrace,
                AdapterName = AdapterName,
                HandlerName = HandlerName,
                InnerException = InnerException?.Clone()
            };
        }

        public InternalExceptionDTO ClearTrace()
        {
            StackTrace = null;
            CallTrace = null;
            return this;
        }
    }
}