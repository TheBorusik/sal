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
    public static class RouteHelper
    {
        private static ConcurrentDictionary<Type, string> routeMap = new ();

        public static string GetSalName(this Type type)
        {
            return  routeMap.GetOrAdd(type, AddValueFactory);
        }
        
        private static string AddValueFactory(Type type)
        {
            if (type.IsAssignableTo<IEvent>())
                return ProcessEvent(type);
            return ProcessCommand(type);
        }


        private static string ProcessCommand(Type type)
        {
            return type.GetAttribute<SalCommandNameAttribute>()?.Name;
        }


        private static string ProcessEvent(Type type)
        {
            return type.GetAttribute<SalEventNameAttribute>()?.Name;
        }

        public static MethodInfo GetMethodByInterfaceMethodInfo(this Type type, MethodInfo interfaceMethodInfo)
        {
            return type.GetMethod(interfaceMethodInfo.Name, interfaceMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray());
        }
    }
}