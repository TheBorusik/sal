using SAL.Infrastructure;

namespace SAL.API
{
    [SalEventName("System.IAmOfflineEvent")]
    public class IAmOfflineEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}