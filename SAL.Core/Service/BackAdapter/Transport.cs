using Autofac;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        protected void StartTransport()
        {
            var backTransport = Container.Resolve<ITransport>();
            backTransport.Start();
           if(Container.TryResolveNamed("front",typeof(ITransport),out var frontTransport)) 
               ((ITransport)frontTransport).Start();
        }

        protected void StopTransport()
        {
            var backTransport = Container.Resolve<ITransport>();
            backTransport.Stop();
            if (Container.TryResolveNamed("front", typeof(ITransport), out var frontTransport))
                ((ITransport)frontTransport).Stop();
        }
    }
}
