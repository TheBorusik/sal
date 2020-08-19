using Autofac;
using SAL.API;
using SAL.API.Client;
using SAL.Core.Client;
using SAL.Core.DB;
using SAL.Core.Processors;
using SAL.Core.Processors.System;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.SystemEventHandlers;
using SAL.Core.Validators;
using SAL.Core.WatchDog;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {

        protected virtual void ConfigureBuilder(ContainerBuilder builder)
        {
            ConfigureContainer(builder);
            ConfigureModules(builder);
        }

        protected virtual void ConfigureContainer(ContainerBuilder builder)
        {
            logger.Trace("ConfigureContainer...");



            builder.Register(c => ConfigWatcher).As<IConfigWatcher>().SingleInstance();
            builder.RegisterType<WatchDog.WatchDog>().As<IWatchDog>().SingleInstance();
            builder.RegisterType<DbConnectionCreator>().As<IDbConnectionCreator>().SingleInstance();
            builder.RegisterType<ObjectValidator>().AsSelf().SingleInstance();


            builder.RegisterType<SalClient>()
                .As<ISalClient>()
                .As<ILoSalClient>()
                .WithParameter("prefix", "");


            builder.RegisterType<SalHandlerLogger>()
                .As<ISalLogger>()
                .SingleInstance();


            builder.RegisterType<CommandProcessor>().AsProcessor();
            builder.RegisterType<CommandResultProcessor>().AsProcessor();
            builder.RegisterType<EventProcessor>().AsProcessor();

      //      builder.RegisterType<HeartbeatBackProcessor>().AsProcessor();

            builder.RegisterSalHandler<SystemWIWHandler>();

            builder.RegisterType<RabbitMQTransport>()
                .As<ITransport>()
                .WithParameter("prefix", "")
                .SingleInstance();

            builder.RegisterType<BackTransportMonitor>()
                .As<IWatchDogMonitor>()
                .SingleInstance();


            AdapterConfigureContainer(builder);

            logger.Trace("ConfigureContainer Done.");
        }

        protected virtual void AdapterConfigureContainer(ContainerBuilder builder)
        {


        }


    }
}