using Autofac;
using SAL.API;
using SAL.Core.Client;
using SAL.Core.Processors;
using SAL.Core.Processors.System;
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

            builder.RegisterType<FrontCommandProcessor>().AsProcessor();
            builder.RegisterType<FrontCommandResultProcessor>().AsProcessor()
                .Named<ICommandResultProcessor>("front");
            builder.RegisterType<FrontEventProcessor>().AsProcessor();
            builder.RegisterType<FrontEventProcessor>().AsProcessor();


            builder.RegisterType<HeartbeatFrontProcessor>().AsProcessor();


        }
    }
}