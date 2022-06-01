using System;
using System.Linq;
using Autofac;
using Autofac.Builder;



namespace SAL.API
{
    public static partial class AutofacHelper
    {
        public static IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterSalHandler<T>(this ContainerBuilder builder)
        {
            var handlerType = typeof(T);
            var registration = builder.RegisterType(handlerType);
            ConfigureRgistration(registration, handlerType);
            return registration;
        }

        public static IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterSalHandler(this ContainerBuilder builder, Type handlerType)
        {
            var registration = builder.RegisterType(handlerType);
            ConfigureRgistration(registration, handlerType);
            return registration;
        }

        private static void ConfigureRgistration(IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration, Type handlerType)
        {
            var interfaces = handlerType.GetInterfaces();
            bool anyHandler = false;
            if (interfaces.Any(i => i.IsAssignableTo<ICommandHandler2>()))
            {
                registration = registration.As<ICommandHandler2>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonCommandHandler2>()))
            {
                registration = registration.As<ICommonCommandHandler2>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommandResultHandler2>()))
            {
                registration = registration.As<ICommandResultHandler2>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonCommandResultHandler2>()))
            {
                registration = registration.As<ICommonCommandResultHandler2>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<IEventHandler2>()))
            {
                registration = registration.As<IEventHandler2>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonEventHandler2>()))
            {
                registration = registration.As<ICommonEventHandler2>();
                anyHandler = true;
            }


            if (interfaces.Any(i => i.IsAssignableTo<IFrontCommandHandler2>()))
            {
                registration = registration.As<IFrontCommandHandler2>();
                anyHandler = true;
            }
            
            if (interfaces.Any(i => i.IsAssignableTo<IFrontCommonCommandHandler2Async>()))
            {
                registration = registration.As<IFrontCommonCommandHandler2Async>();
                anyHandler = true;
            }


            if (interfaces.Any(i => i.IsAssignableTo<IFrontExternalHttpMethod>()))
            {
                registration = registration.As<IFrontExternalHttpMethod>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<IWfmResultHandler>()))
            {
                registration = registration.As<IWfmResultHandler>();
                anyHandler = true;
            }
            
            if (interfaces.Any(i => i.IsAssignableTo<ICommonCommandSharedResultHandler>()))
            {
                registration = registration.As<ICommonCommandSharedResultHandler>();
                anyHandler = true;
            }
            
            if (anyHandler == false)
                throw new System.Exception($"{handlerType.Name} - Не реализует ни одного извесного обработчика");

            registration = registration.AsSelf();
        }

        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterProcessor<T>(this ContainerBuilder builder)
        {
            return builder.RegisterType<T>()
                .AsProcessor()
                .SingleInstance();
        }

        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> AsProcessor<T>(this IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder)
        {
            return builder
                .As<IProcessor>()
                .SingleInstance();
        }

        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRepository<T, TI>(this ContainerBuilder builder) where TI : IDisposable, IBaseRepository
        {
            return builder.RegisterType<T>()
                .As<IBaseRepository<TI>>()
                .As<TI>();
        }
    }
}