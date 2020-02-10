using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Infrastructure.ValidationAttribute;

namespace SAL.Core.Validators
{
    public class ObjectValidator
    {

        public async Task<IList<FieldError>> ValidateData(object dto, Func<object, Task<IEnumerable<FieldError>>> internalValidator)
        {
            var valErrors = Validatedata(dto, null);
            valErrors.AddRange(await internalValidator(dto));
            return valErrors;
        }

        public List<FieldError> ValidateData(object dto)
        {
            return Validatedata(dto, null);
        }

        private List<FieldError> Validatedata(object dto, string[] dtdPath)
        {
            var errors = new List<FieldError>();

            if (dto is JObject)
                return errors;

            var curDtdPath = dtdPath != null ? new List<string>(dtdPath) : new List<string>();
            if (!curDtdPath.Any())
                curDtdPath.Add("request");

            if (dto is null)
            {
                errors.Add(new FieldError
                {
                    Path = string.Join(".", curDtdPath),
                    ErrorCode = ValidationCode.RequiredElementMissing,
                    FieldName = "Body",
                    Description = "Root element: is not recognized."
                });
                return errors;
            }


            var type = dto.GetType();

            var properties = type.GetProperties();
            foreach (var propertyInfo in properties)
            {
                var fVal = propertyInfo.GetValue(dto);
                object defVal = null;
                var pType = propertyInfo.PropertyType;


                if (pType.IsValueType)
                {
                    try
                    {
                        defVal = Activator.CreateInstance(pType);
                    }
                    catch (Exception)
                    {
                        defVal = null;
                    }
                }

                if (fVal is string)
                {
                    defVal = "";
                }

                if (fVal is null || Equals(fVal, defVal))
                {
                    if (propertyInfo.GetCustomAttributes(typeof(RequiredAttribute), false).OfType<RequiredAttribute>().Any())
                    {
                        errors.Add(new FieldError
                        {
                            Path = $"{string.Join(".", curDtdPath)}.{propertyInfo.Name}",
                            ErrorCode = ValidationCode.RequiredElementMissing,
                            FieldName = propertyInfo.Name,
                            Description = "Field is missing. The content of this field should be specified."
                        });
                    }
                    continue;
                }

                if ((fVal as Array)?.Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(NotEmptyArrayAttribute), false).OfType<NotEmptyArrayAttribute>().Any())
                    {
                        errors.Add(new FieldError
                        {
                            Path = $"{string.Join(".", curDtdPath)}.{propertyInfo.Name}",
                            ErrorCode = ValidationCode.RequiredElementMissing,
                            FieldName = propertyInfo.Name,
                            Description = "Array is empty. The content of this array should be specified."
                        });
                        continue;
                    }
                }

                if (pType.IsArray)
                {

                    var isCRLElement = IsCLRType(pType.GetElementType());

                    var enumerator = ((Array)fVal).GetEnumerator();
                    var index = -1;

                    while (enumerator.MoveNext())
                    {
                        index++;
                        var copy = curDtdPath.ToList();
                        copy.Add(propertyInfo.Name);
                        copy.Add($"[{index}]");

                        if (!isCRLElement)
                            errors.AddRange(Validatedata(enumerator.Current, copy.ToArray()));

                    }
                    continue;
                }

                if (!IsCLRType(pType))
                {
                    var copy = curDtdPath.ToList();
                    copy.Add(propertyInfo.Name);
                    errors.AddRange(Validatedata(fVal, copy.ToArray()));
                }

            }

            return errors;

        }

        bool IsCLRType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsCLRType(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
                   || type.IsEnum
                   || type.Module.ScopeName == "CommonLanguageRuntimeLibrary"
                ;
        }
    }
}
