using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API
{
    [SalSystemEvent]
    [SalServiceType("System")]
    [SalEventName("ExceptionDetected")]
    public class ExceptionDetectedEvent : IEvent
    {
        public string ServiceType { get; set; }
        public string ServiceName { get; set; }
        public InternalExceptionDTO ExceptionDto { get; set; }
    }
}
