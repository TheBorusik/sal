using Autofac;
using SAL.API;
using SAL.API.Client;
using SAL.Core.Client;
using SAL.Core.DB;
using SAL.Core.Processors;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Validators;
using SAL.Core.WatchDog;

namespace SAL.Core.Service
{
    public partial class AdapterRunner
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
                .As<ILoSalClient>();


            builder.RegisterType<SalHandlerLogger>()
                .As<ISalLogger>()
                .SingleInstance();



            //todo придумать получше
            BackConfigureContainer(builder);
            FrontConfigureContainer(builder);

            logger.Trace("ConfigureContainer Done.");
        }

        protected virtual void FrontConfigureContainer(ContainerBuilder builder)
        {

        }

        protected virtual void BackConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<CommandProcessor>().AsProcessor();
            builder.RegisterType<CommandResultProcessor>().AsProcessor();
            builder.RegisterType<EventProcessor>().AsProcessor();


            builder.RegisterType<RabbitMQTransport>()
                .As<ITransport>()
                .AsSelf()
                .WithParameter("prefix", "")
                .SingleInstance();

            builder.RegisterType<TransportMonitor>()
                .As<IWatchDogMonitor>()
                .SingleInstance();

            builder.RegisterType<RabbitMQPublisher>()
                .As<IPublisher>()
                .SingleInstance();
        }


    }
}