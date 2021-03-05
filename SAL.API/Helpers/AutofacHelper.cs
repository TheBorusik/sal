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
            if (interfaces.Any(i => i.IsAssignableTo<ICommandHandler>()))
            {
                registration = registration.As<ICommandHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonCommandHandler>()))
            {
                registration = registration.As<ICommonCommandHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommandResultHandler>()))
            {
                registration = registration.As<ICommandResultHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonCommandResultHandler>()))
            {
                registration = registration.As<ICommonCommandResultHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<IEventHandler>()))
            {
                registration = registration.As<IEventHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<ICommonEventHandler>()))
            {
                registration = registration.As<ICommonEventHandler>();
                anyHandler = true;
            }


            if (interfaces.Any(i => i.IsAssignableTo<IFrontCommandHandler>()))
            {
                registration = registration.As<IFrontCommandHandler>();
                anyHandler = true;
            }

            if (interfaces.Any(i => i.IsAssignableTo<IFrontCommonCommandHandlerAsync>()))
            {
                registration = registration.As<IFrontCommonCommandHandlerAsync>();
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