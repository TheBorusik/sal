using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    public interface IEventHandler2
    {
    }
    
    public interface IEventHandler2<in TEvent> : IEventHandler2
        where TEvent : class, IEvent, new()
    {
        Task Handle(TEvent evnt, EventContext eventContext, ExecutingContext executingContext);
    }
}