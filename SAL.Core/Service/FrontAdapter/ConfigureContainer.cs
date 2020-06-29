using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;
using Autofac;
using Autofac.Core;
using SAL.API;
using SAL.API.Client;
using SAL.Core.Client;
using SAL.Core.Processors;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.WatchDog;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected override void AdapterConfigureContainer(ContainerBuilder builder)
        {

            builder.RegisterType<RabbitMQTransport>()
                .Named<ITransport>("front")
                .AsSelf()
                .WithParameter("prefix", "front")
                .SingleInstance();

            builder.RegisterType<SalClient>()
                .Named<ISalClient>("front")
                .Named<ILoSalClient>("front")
                .WithParameter("prefix", "front");

            builder.RegisterType<FrontTransportMonitor>()
                .As<IWatchDogMonitor>()
                .SingleInstance();


        }
    }
}