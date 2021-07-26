using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAL.API
{
    internal static class AttributesHelper
    {
        public static T GetAttribute<T>(this Type type)
        {
            return type.GetCustomAttributes(typeof(T)).OfType<T>().FirstOrDefault();
        }
        
        public static IEnumerable<T> GetAttributes<T>(this Type type)
        {
            return type.GetCustomAttributes(typeof(T)).OfType<T>();
        }
        
        public static bool HasAttribute<T>(this Type type)
        {
            return type.GetCustomAttributes(typeof(T)).Any();
        }
        
    }
}