using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class SalError
    {
        public static SalException CreateException(
            string code,
            string message = "",
            object properties = null,
            System.Exception innerException = null)
        {
            return CreateDto(code, message, properties, innerException).ToException();
        }

        
        public static InternalExceptionDTO CreateDto(
            string code, 
            string message = "",
            object properties = null, 
            System.Exception innerException = null)
        {
            var dto = new InternalExceptionDTO
            {
                Code = code,
                TimeStamp = DateTime.UtcNow,
                Message = message,
                Properties = properties != null ? JObject.FromObject(properties) : new JObject(),
                AdapterName = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}",
                StackTrace = innerException != null ? Environment.StackTrace : null,
                HandlerName = $"{HandlerContext.HandlerType}.{HandlerContext.HandlerName}",
                SessionId = HandlerContext.SessionId,
                CorrelationId = HandlerContext.CorrelationId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                InnerException = innerException.ToDto(),
            };

            return dto;
        }
        

        

        //
        //
        public static InternalExceptionDTO ToDto(this System.Exception ex, 
            string code = SalErrorCodes.Fatal,
            object properties = null)
        {
            if (ex == null)
                return null;

            InternalExceptionDTO dto;
            if (ex is SalException sex)
            {
                dto = sex.ToDto();
                if (properties != null)
                {
                    dto.Properties.Merge(JObject.FromObject(properties), new JsonMergeSettings{MergeArrayHandling = MergeArrayHandling.Merge});
                }
                return dto;
            }
 
            dto = new InternalExceptionDTO
            {
                Code = code,
                TimeStamp = DateTime.UtcNow,
                Message = ex.Message,
                ExceptionType = ex.GetType().Name,
                Properties = properties != null ? JObject.FromObject(properties) : new JObject(),
                AdapterName = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}",
                StackTrace = ex.StackTrace,
                HandlerName = $"{HandlerContext.HandlerType}.{HandlerContext.HandlerName}",
                SessionId = HandlerContext.SessionId,
                CorrelationId = HandlerContext.CorrelationId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                InnerException = ex.InnerException.ToDto(SalErrorCodes.Fatal)
            };





            return dto;
        }


        public static InternalExceptionDTO ToDto(this SalException ex)
        {
            return ex.Dto.Clone();
        }

        public static SalException ToException(this InternalExceptionDTO dto)
        {
            return dto.InnerException == null ? 
                new SalException(dto) : 
                new SalException(dto, dto.InnerException.ToException());
        }


        public static T GetProperty<T>(this ExceptionDTO dto, string keyName = null)
        {
            if (dto.Properties == null)
                throw  new System.Exception("Properties Is NULL");

            return string.IsNullOrWhiteSpace(keyName) ? 
                dto.Properties.ToObject<T>() : 
                dto.Properties.GetValue<T>(keyName);
        }

        public static T GetProperty<T>(this SalException ex, string keyName = null)
        {
            return GetProperty<T>(ex.Dto, keyName);
        }

        public static T GetSafeProperty<T>(this ExceptionDTO dto, T defaultValue, string keyName = null)
        {
            if (dto.Properties == null)
                return defaultValue;

            return string.IsNullOrWhiteSpace(keyName) ?
                dto.Properties.ToObject<T>() :
                dto.Properties.GetSafeValue(keyName, defaultValue);
        }


        public static InternalExceptionDTO CreateValidationDto(IList<FieldError> validationErrors)
        {
            var dto = new InternalExceptionDTO
            {
                Code = SalErrorCodes.ValidationFailed,
                Message = string.Join(Environment.NewLine,
                    validationErrors.Select(v => string.IsNullOrWhiteSpace(v.Path) ? v.Description : $"{v.Description}. Path '{v.Path}'")),
                TimeStamp = DateTime.UtcNow,
                Properties = JObject.FromObject(new { ValidationErrors  = validationErrors }) ,
                AdapterName = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}",
                HandlerName = $"{HandlerContext.HandlerType}.{HandlerContext.HandlerName}",
                SessionId = HandlerContext.SessionId,
                CorrelationId = HandlerContext.CorrelationId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId
            };

            return dto;
        }
    }
}