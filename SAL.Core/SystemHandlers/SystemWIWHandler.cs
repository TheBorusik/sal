using System.Threading.Tasks;
using SAL.API;
using SAL.API.Const;
using SAL.Core.Service;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [SalContourHandler(Contour.Both)]

    class SystemWIWHandler : IEventHandler2<WhoIsWhoEvent>
    {
        private ISalService salService;


        public SystemWIWHandler(ISalService salService)
        {
            this.salService = salService;
        }
        [SalEventName("System.WhoIsWhoEvent", false, false)]
        public Task Handle(WhoIsWhoEvent evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            if (eventContext.CheckIsMyEvent())
                return Task.CompletedTask;

            if (eventContext.CheckIsExpire(SalConst.SystemEventTTL))
                return Task.CompletedTask;
            

            salService.SendIm(executingContext.SalClient.Contour);
            return Task.CompletedTask;
        }


    }
}
