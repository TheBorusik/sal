using Autofac;
using SAL.API;
using SAL.Core.Client;
using SAL.Core.Processors;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.WatchDog;
using SAL.Infrastructure;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected override void AdapterConfigureContainer(ContainerBuilder builder)
        {

            builder.RegisterType<RabbitMQTransportAsync>()
                .Keyed<ITransport>(Contour.Front)
                .WithParameter("contour", Contour.Front)
                .SingleInstance();

            builder.RegisterType<SalClient>()
                .Keyed<ISalClient>(Contour.Front)
                .Keyed<ILoSalClient>(Contour.Front)
                .WithParameter("contour", Contour.Front);

            builder.RegisterType<FrontTransportMonitor>()
                .As<IWatchDogMonitor>()
                .SingleInstance();

            builder.RegisterType<FrontCommandProcessor>().AsProcessor();
            builder.RegisterType<FrontCommandResultProcessor>().AsProcessor()
                .Keyed<ICommandResultProcessor>(Contour.Front);
            builder.RegisterType<FrontEventProcessor>().AsProcessor();
            
            builder.RegisterType<FrontExternalHttpProcessor>().AsProcessor();


            builder.RegisterType<HeartbeatFrontProcessor>().AsProcessor();


        }
    }
}