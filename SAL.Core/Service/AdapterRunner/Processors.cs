using Autofac;
using SAL.API;
using SAL.API.Monad;

namespace SAL.Core.Service
{
    public partial class AdapterRunner
    {
        private IProcessor[] processors;


        protected void InitProcessors()
        {
            processors = Container.Resolve<IProcessor[]>();
        }

        protected void StartProcessors()
        {
            HandlerContext.Type = HandlerTypes.Processor;
            processors.ForEach(p =>
            {
                HandlerContext.Name = p.GetType().Name;
                p.Start();
            });
        }

        protected void StopProcessors()
        {
            HandlerContext.Type = HandlerTypes.Processor;
            processors.TryForEach(p =>
            {
                HandlerContext.Name = p.GetType().Name;
                p.Stop();
            });
        }

        protected void OnlineProcessors()
        {

            HandlerContext.Type = HandlerTypes.Processor;
            processors.ForEach(p =>
            {
                HandlerContext.Name = p.GetType().Name;
                p.Online();
            });

        }

        protected void OfflineProcessors()
        {
            HandlerContext.Type = HandlerTypes.Processor;
            processors.ForEach(p =>
            {
                HandlerContext.Name = p.GetType().Name;
                p.Offline();
            });
        }
    }
}