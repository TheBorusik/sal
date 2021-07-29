using SAL.Infrastructure;

namespace SAL.API
{
    [SalEventName("System.ExceptionDetected")]
    public class ExceptionDetectedEvent : IEvent
    {
        public string CorrelationId { get; set; }
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public InternalExceptionDTO ExceptionDto { get; set; }
    }
}
