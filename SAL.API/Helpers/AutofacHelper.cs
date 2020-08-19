using System;
using System.Linq;
using Autofac;
using Autofac.Builder;
using SAL.API.Command;
using SAL.API.CommandResult;
using SAL.API.Events;
using SAL.API.FrontCommand;

namespace SAL.API
{
    public static partial class AutofacHelper
    {
        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterSalHandler<T>(this ContainerBuilder builder)
        {
            var handlerType = typeof(T);


            bool anyHandler = false;
            var registration = builder.RegisterType<T>();

            var interfaces = handlerType.GetInterfaces();

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

            if (anyHandler == false)
                throw new System.Exception($"{handlerType.Name} - Не реализует ни одного извесного обработчика");


            registration = registration.AsSelf();


            return registration;
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
                .AsImplementedInterfaces()
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
