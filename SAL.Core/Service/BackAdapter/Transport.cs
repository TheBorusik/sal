using System;
using Autofac;
using SAL.Core.Rabbit.Interfaces;
using SAL.Infrastructure;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        protected void StartTransport()
        {
            var backTransport = Container.Resolve<ITransport>();
            try
            {
                backTransport.Start();
            }
            catch (Exception e)
            {
                throw new Exception($"Back BUS Error: {e.Message}");
            }

            if (Container.TryResolveKeyed(Contour.Front, typeof(ITransport), out var frontTransport))
            {
                try
                {
                    ((ITransport)frontTransport).Start();
                }
                catch (Exception e)
                {
                    throw new Exception($"Front BUS Error: {e.Message}");
                }
            }
                
                

        }

        protected void StopTransport()
        {
            var backTransport = Container.Resolve<ITransport>();
            backTransport.Stop();
            if (Container.TryResolveKeyed(Contour.Front, typeof(ITransport), out var frontTransport))
                ((ITransport)frontTransport).Stop();
        }
    }
}
