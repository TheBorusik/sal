using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalEventName("ExceptionDetected")]
    public class ExceptionDetectedEvent : IEvent
    {
        public string CorrelationId { get; set; }
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public InternalExceptionDTO ExceptionDto { get; set; }
    }
}
