using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using SAL.API;
using SAL.Infrastructure;

namespace SAL.Core.Helpers
{
    internal class RouteKeyMap
    {
        public string Name;
        public string RequestType;
        public bool IsSystemEvent;
    }

    public static class RouteHelper
    {
        private static ConcurrentDictionary<Type, RouteKeyMap> routeMap = new ConcurrentDictionary<Type, RouteKeyMap>();

        public static string GetSalName(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.Name;
        }

        public static string GetRequestType(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.RequestType;
        }

        public static bool IsSystemEvent(this Type type)
        {
            var map = routeMap.GetOrAdd(type, AddValueFactory);
            return map.IsSystemEvent;
        }

        
        private static RouteKeyMap AddValueFactory(Type type)
        {
            if (type.IsAssignableTo<IEvent>())
                return ProcessEvent(type);
            return ProcessCommand(type);
        }



        private static RouteKeyMap ProcessCommand(Type type)
        {
            var scnAttribute = type.GetAttribute<SalCommandNameAttribute>();
            
            if (scnAttribute == null)
            {
                throw new Exception($"Не заданно значение SalCommandNameAttribute для типа {type.Name}.");
            }
            
            return new RouteKeyMap
            {
                Name = scnAttribute.Name,
                IsSystemEvent = false,
                RequestType = type.GetAttribute<SalRequestTypeAttribute>()?.RequestType
            };
        }
        
        
        private static RouteKeyMap ProcessEvent(Type type)
        {
            var name = string.Empty;
            var evnAttribute = type.GetAttribute<SalEventNameAttribute>();
            if (evnAttribute == null)
            {
                throw new Exception($"Не заданно значение SalEventNameAttribute для типа {type.Name}.");
            }

            name = evnAttribute.Name;
            
            var isSystem = type.GetCustomAttributes(typeof(SalSystemEventAttribute)).Any();

            return new RouteKeyMap
            {
                Name = isSystem ? $"System.{name}" : name,
                IsSystemEvent = isSystem
            };
        }
    }
}