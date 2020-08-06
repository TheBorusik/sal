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

        public SystemWIWHandler(ISalService salService)
        {
            this.salService = salService;
        }


        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {

        }

        public Task Handle(WhoIsWhoEvent evnt)
        {
            salService.SendIm();
            return Task.CompletedTask;
        }


    }
}
