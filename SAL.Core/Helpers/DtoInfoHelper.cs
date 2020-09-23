using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Infrastructure.ValidationAttribute;
using FieldInfo = SAL.API.FieldInfo;

namespace SAL.Core.Helpers
{
    public static class DtoInfoHelper
    {
        public static DtoInfo[] GetDtoInfos(this Type dtoType)
        {
            var list = new List<DtoInfo>();
            var fieldList = new List<FieldInfo>();

            var dtoInfo = new DtoInfo
            {
                Name = dtoType.Name
            };

            list.Add(dtoInfo);

            foreach (var pi in dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldRequired = pi.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));

                var pft = GetFieldType(pi.PropertyType);
                switch (pft)
                {
                    case FieldType.Object:
                    {
                        if (pi.PropertyType == typeof(object) || pi.PropertyType.IsAssignableTo<JToken>())
                        {
                            fieldList.Add(new FieldInfo
                            {
                                Type = pft,
                                IsRequired = fieldRequired,
                                Name = pi.Name,
                            });
                        }
                        else
                        {

                            if (list.All(di => di.Name != pi.PropertyType.Name))
                            {

                                var objDtoInfos = pi.PropertyType.GetDtoInfos();
                                AddDtoInfo(list, objDtoInfos);
                                fieldList.Add(new FieldInfo
                                {
                                    Type = pft,
                                    IsRequired = fieldRequired,
                                    Name = pi.Name,
                                    ObjectName = objDtoInfos.First().Name
                                });
                            }
                        }

                        break;
                    }
                    case FieldType.Dictionary:
                    {
                        var fieldInfo = new FieldInfo
                        {
                            Type = pft,
                            IsRequired = fieldRequired,
                            Name = pi.Name,
                        };

                        var elementType = pi.PropertyType.GetGenericArguments()[1];
                        fieldInfo.ElementType = GetFieldType(elementType);

                        if (elementType != typeof(object) && !elementType.IsAssignableTo<JToken>())
                        {
                            if (fieldInfo.ElementType == FieldType.Object)
                            {
                                var elementDtoInfos = elementType.GetDtoInfos();
                                fieldInfo.ElementObjectName = elementDtoInfos.First().Name;
                                AddDtoInfo(list, elementDtoInfos);
                            }
                        }

                        fieldList.Add(fieldInfo);

                        break;
                    }
                    case FieldType.Array:
                    {
                        var fieldInfo = new FieldInfo
                        {
                            Type = pft,
                            IsRequired = fieldRequired,
                            Name = pi.Name,
                        };

                        var elementType = pi.PropertyType.GetElementType();
                        fieldInfo.ElementType = GetFieldType(elementType);
                        if (elementType != typeof(object) && !elementType.IsAssignableTo<JToken>())
                        {
                            if (fieldInfo.ElementType == FieldType.Object)
                            {
                                var elementDtoInfos = elementType.GetDtoInfos();
                                fieldInfo.ElementObjectName = elementDtoInfos.First().Name;
                                AddDtoInfo(list, elementDtoInfos);
                            }
                        }

                        fieldList.Add(fieldInfo);
                        break;
                    }
                    case FieldType.Integer:
                    case FieldType.Float:
                    case FieldType.String:
                    case FieldType.Boolean:
                    case FieldType.Bytes:
                    case FieldType.Guid:
                    case FieldType.Date:
                    case FieldType.TimeSpan:
                        fieldList.Add(new FieldInfo {Type = pft, Name = pi.Name, IsRequired = fieldRequired});
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            dtoInfo.FieldsInfos = fieldList.ToArray();

            return list.ToArray();
        }

        private static void AddDtoInfo(List<DtoInfo> list, DtoInfo info)
        {
            if (list.Any(i => i.Name == info.Name))
                return;
            list.Add(info);
        }

        private static void AddDtoInfo(List<DtoInfo> list, DtoInfo[] infos)
        {
            foreach (var dtoInfo in infos)
            {
                AddDtoInfo(list, dtoInfo);
            }
        }

        private static FieldType GetFieldType(Type t)
        {
            if (t == typeof(byte[]))
                return FieldType.Bytes;

            if (t.IsArray)
                return FieldType.Array;

            if (t == typeof(string))
                return FieldType.String;
            if (t == typeof(Uri))
                return FieldType.String;

            if (t == typeof(DateTime))
                return FieldType.Date;
            if (t == typeof(TimeSpan))
                return FieldType.TimeSpan;
            if (t == typeof(Guid))
                return FieldType.Guid;

            if (t == typeof(bool))
                return FieldType.Boolean;


            if (t == typeof(int))
                return FieldType.Integer;
            if (t == typeof(uint))
                return FieldType.Integer;
            if (t == typeof(long))
                return FieldType.Integer;
            if (t == typeof(ulong))
                return FieldType.Integer;

            if (t == typeof(float))
                return FieldType.Float;
            if (t == typeof(double))
                return FieldType.Float;
            if (t == typeof(decimal))
                return FieldType.Float;

            if (t.IsGenericType)
            {
                var genericType = t.GetGenericTypeDefinition();
                if (genericType == typeof(Dictionary<,>))
                    return FieldType.Dictionary;
            }


            if (t.IsValueType)
                return FieldType.String;

            return FieldType.Object;
        }
    }
}