using System.Threading.Tasks;
using SAL.API;
using SAL.Core.Service;

namespace SAL.Core.SystemHandlers
{
    class SystemWIWHandler : IEventHandler<WhoIsWhoEvent>
    {
        private ISalService salService;
        private EventContext context;
        private ExecutingContext executingContext;

        public SystemWIWHandler(ISalService salService)
        {
            this.salService = salService;
        }


        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {
            this.context = eventContext;
            this.executingContext = executingContext;
        }

        public Task Handle(WhoIsWhoEvent evnt)
        {
            if (context.CheckIsMyEvent())
                return Task.CompletedTask;

            if (context.CheckIsExpire(SystemEventTimes.BaseTTL))
                return Task.CompletedTask;
            

            salService.SendIm(executingContext.SalClient.Contour);
            return Task.CompletedTask;
        }


    }
}
