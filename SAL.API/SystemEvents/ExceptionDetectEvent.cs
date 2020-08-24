using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("ExceptionDetected")]
    public class ExceptionDetectedEvent : IEvent
    {
        public string CorrelationId { get; set; }
        public string ServiceType { get; set; }
        public string ServiceName { get; set; }
        public InternalExceptionDTO ExceptionDto { get; set; }
    }
}
