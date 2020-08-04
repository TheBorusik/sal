using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SAL.API;
using SAL.API.Events;

namespace SAL.Core.SystemEventHandlers
{
    class SystemWIWHandler : IEventHandler<WhoIsWhoEvent>
    {
        public Task Handle(WhoIsWhoEvent evnt)
        {
            throw new NotImplementedException();
        }

        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {
            throw new NotImplementedException();
        }
    }
}
