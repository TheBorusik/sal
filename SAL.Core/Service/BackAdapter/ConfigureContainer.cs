using Autofac;
using SAL.API;
using SAL.Core.Client;
using SAL.Core.Configuration.Redis;
using SAL.Core.DB;
using SAL.Core.DB.RedisStore;
using SAL.Core.Processors;
using SAL.Core.Processors.System;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.S3;
using SAL.Core.Session;
using SAL.Core.SystemHandlers;
using SAL.Core.WatchDog;
using SAL.Infrastructure;

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



            builder.Register(_ => ConfigWatcher).As<IConfigWatcher>().SingleInstance();
            builder.RegisterType<WatchDog.WatchDog>().As<IWatchDog>().SingleInstance();
            builder.RegisterType<DbConnectionCreator>().As<IDbConnectionCreator>().SingleInstance();



            builder.RegisterType<SalClient>()
                .As<ISalClient>()
                .As<ILoSalClient>()
                .Keyed<ISalClient>(Contour.Back)
                .Keyed<ILoSalClient>(Contour.Back)
                .WithParameter("contour", Contour.Back);


            builder.RegisterType<SalHandlerLogger>()
                .As<ISalLogger>()
                .SingleInstance();


            builder.RegisterType<CommandProcessor>().AsProcessor();
            
            builder.RegisterType<CommandResultProcessor>().AsProcessor()
                .As<ICommandResultProcessor>();
            builder.RegisterType<EventProcessor>().AsProcessor();

            builder.RegisterType<HeartbeatBackProcessor>().AsProcessor();

            builder.RegisterSalHandler<SystemWIWHandler>();

            builder.RegisterSalHandler<StopAdapterCommandHandler>();

            builder.RegisterType<RabbitMQTransportAsync>()
                .As<ITransport>()
                .Keyed<ITransport>(Contour.Back)
                .WithParameter("contour", Contour.Back)
                .SingleInstance();

            builder.RegisterType<BackTransportMonitor>()
                .As<IWatchDogMonitor>()
                .SingleInstance();
            
            
            var redisConfig = ConfigWatcher.GetSection(ConfigurationSectionNames.RedisStore)?.ConvertValue<RedisStoreConfig>();
            if (redisConfig != null)
            {
                builder.RegisterType<RedisStore>()
                    .As<IRedisStore>()
                    .SingleInstance();


            }
            
            builder.RegisterType<SessionManager>()
                .As<ISessionManager>()
                .SingleInstance();

            builder.RegisterType<S3Store>()
                .As<IS3Store>();

            AdapterConfigureContainer(builder);

            logger.Trace("ConfigureContainer Done.");
        }

        protected virtual void AdapterConfigureContainer(ContainerBuilder builder)
        {


        }


    }
}