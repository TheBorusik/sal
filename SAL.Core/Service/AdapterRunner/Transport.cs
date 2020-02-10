using System.Collections.Generic;
using Autofac;
using SAL.API.Monad;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Service
{
    public partial class AdapterRunner
    {
        protected void StartTransport()
        {
            var transports = Container.Resolve<IEnumerable<ITransport>>();
            transports.ForEach(t => t.Start());
        }

        protected void StopTransport()
        {
            var transports = Container.Resolve<IEnumerable<ITransport>>();
            transports.ForEach(t => t.Stop());
        }
    }
}