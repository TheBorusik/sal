using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.Events
{
    public interface IEventHandler
    {
    }

    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : class, IEvent, new()
    {
        Task Handle(TEvent Event, EventDescriptor eventDescriptor, ExecutingContext executingContext);
    }
}