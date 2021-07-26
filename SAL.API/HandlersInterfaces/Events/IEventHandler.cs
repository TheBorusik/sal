using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete]
    public interface IEventHandler
    {
        void SetContexts(EventContext eventContext, ExecutingContext executingContext);
    }

    [Obsolete]
    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : class, IEvent, new()
    {
        Task Handle(TEvent evnt);
    }


}