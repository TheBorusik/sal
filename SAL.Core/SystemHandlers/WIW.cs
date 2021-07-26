using System.Threading.Tasks;
using SAL.API;
using SAL.Core.Service;

namespace SAL.Core.SystemHandlers
{
    class SystemWIWHandler : IEventHandler2<WhoIsWhoEvent>
    {
        private ISalService salService;


        public SystemWIWHandler(ISalService salService)
        {
            this.salService = salService;
        }
        
        public Task Handle(WhoIsWhoEvent evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            if (eventContext.CheckIsMyEvent())
                return Task.CompletedTask;

            if (eventContext.CheckIsExpire(SystemEventTimes.BaseTTL))
                return Task.CompletedTask;
            

            salService.SendIm(executingContext.SalClient.Contour);
            return Task.CompletedTask;
        }


    }
}
