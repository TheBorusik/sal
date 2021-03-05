using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using SAL.Infrastructure;

namespace SAL.Core.Helpers
{
    internal class RouteKeyMap
    {
        internal Type Type;
        internal string RoutingKey;
        internal string TypeName;
        internal string Name;
        internal bool IsResultTypeHandler;
        internal bool IsSystemEvent;
    }

    public static class RouteHelper
    {
        private static ConcurrentDictionary<Type, RouteKeyMap> routeMap = new ConcurrentDictionary<Type, RouteKeyMap>();

        public static string GetRouteKey(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.RoutingKey;
        }

        public static bool IsSystemEvent(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.IsSystemEvent;
        }


        public static bool IsResultTypeHandler(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.IsResultTypeHandler;
        }

        private static RouteKeyMap AddValueFactory(Type type)
        {
            if (type.IsAssignableTo<ICommand>())
                return ProcessCommand(type);
            if (type.IsAssignableTo<IEvent>())
                return ProcessEvent(type);
            return null;

        }



        private static RouteKeyMap ProcessCommand(Type type)
        {
            var typeName = string.Empty;

            if (type.IsAssignableFrom(typeof(ICommand)))
                return ProcessCommand(type);


            var sstAttribute = type.GetCustomAttributes(typeof(SalServiceTypeAttribute)).OfType<SalServiceTypeAttribute>().FirstOrDefault();
            if (sstAttribute != null)
            {
                typeName = sstAttribute.Type;
            }
            else
            {
                sstAttribute = type.Assembly.GetCustomAttributes(typeof(SalServiceTypeAttribute)).OfType<SalServiceTypeAttribute>().FirstOrDefault();
                if (sstAttribute != null)
                    typeName = sstAttribute.Type;
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new Exception($"Не заданно значение SalServiceTypeAttribute для типа {type.Name}.");
            }

            var name = "";
            var scnAttribute = type.GetCustomAttributes(typeof(SalCommandNameAttribute)).OfType<SalCommandNameAttribute>().FirstOrDefault();
            if (scnAttribute != null)
            {
                name = scnAttribute.Name;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Regex.Replace(type.Name, "(.+)command$", "$1", RegexOptions.IgnoreCase);
            }

            var routeKey = $"{typeName}.{name}";

            var rthAttributePresent = type.GetCustomAttributes(typeof(SalCommandTypeResultHandlerAttribute)).OfType<SalCommandTypeResultHandlerAttribute>().Any();

            return new RouteKeyMap
            {
                Type = type,
                RoutingKey = routeKey,
                TypeName = typeName,
                Name = name,
                IsResultTypeHandler = rthAttributePresent,
                IsSystemEvent = false
            };
        }



        private static RouteKeyMap ProcessEvent(Type type)
        {
            var name = string.Empty;
            var evnAttribute = type.GetCustomAttributes(typeof(SalEventNameAttribute)).OfType<SalEventNameAttribute>().FirstOrDefault();
            if (evnAttribute != null)
            {
                name = evnAttribute.Name;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Regex.Replace(type.Name, "(.+)event", "$1", RegexOptions.IgnoreCase);
            }

            var isSystem = type.GetCustomAttributes(typeof(SalSystemEventAttribute)).Any();

            return new RouteKeyMap
            {
                Type = type,
                RoutingKey = isSystem ? $"System.{name}" : name,
                TypeName = string.Empty,
                Name = name,
                IsResultTypeHandler = false,
                IsSystemEvent = isSystem
            };
        }
    }
}