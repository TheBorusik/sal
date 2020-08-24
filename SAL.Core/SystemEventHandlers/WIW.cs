using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SAL.API;
using SAL.API.Events;
using SAL.Core.Service;

namespace SAL.Core.SystemEventHandlers
{
    class SystemWIWHandler : IEventHandler<WhoIsWhoEvent>
    {
        private ISalService salService;
        private EventContext context;

        public SystemWIWHandler(ISalService salService)
        {
            this.salService = salService;
        }


        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {
            this.context = eventContext;
        }

        public Task Handle(WhoIsWhoEvent evnt)
        {
            if (context.CheckIsMyEvent())
                return Task.CompletedTask;

            if (context.CheckIsExpire(SystemEventTimes.BaseTTL))
                return Task.CompletedTask;
            

            salService.SendIm();
            return Task.CompletedTask;
        }


    }
}
