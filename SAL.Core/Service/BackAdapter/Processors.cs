using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using SAL.API;

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
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                try
                {
                    HandlerContext.Set(HandlerTypes.Processor, p.GetType().Name);
                    p.Start();
                }
                catch (Exception e)
                {
                    throw new Exception($"[{p.GetType().Name}] {e.Message}", e);
                }

            })).ToArray();

            Task.WaitAll(onlineTasks);
            
        }

        protected void StopProcessors()
        {
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                try
                {
                    HandlerContext.Set(HandlerTypes.Processor, p.GetType().Name);
                    p.Stop();
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            })).ToArray();

            Task.WaitAll(onlineTasks);
        }

        protected void OnlineProcessors()
        {
            var onlineTasks = processors.Select(p => Task.Run(() =>
            {
                try
                {
                    HandlerContext.Set(HandlerTypes.Processor, p.GetType().Name);
                    p.Online();
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            })).ToArray();

            Task.WaitAll(onlineTasks);


        }

        protected void OfflineProcessors()
        {
            var offlineTasks = processors.Select(p => Task.Run(() =>
            {
                try
                {
                    HandlerContext.Set(HandlerTypes.Processor, p.GetType().Name);
                    p.Offline();
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            })).ToArray();
            
            Task.WaitAll(offlineTasks);
        }
    }
}