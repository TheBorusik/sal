using System;
using System.Threading.Tasks;
using SAL.Infrastructure;

namespace SAL.API
{
    [Obsolete("Use IEventHandler2")]
    public interface IEventHandler
    {
        void SetContexts(EventContext eventContext, ExecutingContext executingContext);
    }

    [Obsolete("Use IEventHandler2")]
    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : class, IEvent, new()
    {
        Task Handle(TEvent evnt);
    }


}