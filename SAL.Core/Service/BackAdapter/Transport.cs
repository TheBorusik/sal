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
            backTransport.Start();
           if(Container.TryResolveKeyed(Contour.Front,typeof(ITransport),out var frontTransport)) 
               ((ITransport)frontTransport).Start();
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
