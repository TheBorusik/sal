using System.Linq;
using System.Threading.Tasks;
using Autofac;
using SAL.API;
using SAL.API.Monad;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        private IProcessor[] processors;


        protected void InitProcessors()
        {
            processors = Container.Resolve<IProcessor[]>();
        }

        protected void StartProcessors()
        {
            HandlerContext.Type = HandlerTypes.Processor;
            
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = p.GetType().Name;
                p.Start();
            })).ToArray();

            Task.WaitAll(onlineTasks);
            
        }

        protected void StopProcessors()
        {
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = p.GetType().Name;
                p.Stop();
            })).ToArray();

            Task.WaitAll(onlineTasks);
        }

        protected void OnlineProcessors()
        {
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = p.GetType().Name;
                p.Online();
            })).ToArray();

            Task.WaitAll(onlineTasks);


        }

        protected void OfflineProcessors()
        {
            var offlineTasks = processors.Select(p => Task.Run(() =>
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = p.GetType().Name;
                p.Offline();
            })).ToArray();
            
            Task.WaitAll(offlineTasks);
        }
    }
}