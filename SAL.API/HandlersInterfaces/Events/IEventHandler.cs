using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API.Events
{
    public interface IEventHandler
    {
        void SetContexts(EventContext eventContext, ExecutingContext executingContext);
    }

    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : class, IEvent, new()
    {
        Task Handle(TEvent evnt);
    }
}