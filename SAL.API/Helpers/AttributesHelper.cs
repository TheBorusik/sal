using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAL.API
{
    public static class AttributesHelper
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
        
        public static T GetAttribute<T>(this MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(T)).OfType<T>().FirstOrDefault();
        }
        
        public static IEnumerable<T> GetAttributes<T>(this MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(T)).OfType<T>();
        }
        
        public static bool HasAttribute<T>(this MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(T)).Any();
        }
        
    }
}