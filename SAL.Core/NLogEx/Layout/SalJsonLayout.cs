using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Layouts;
using SAL.API;
using SAL.Infrastructure;

namespace SAL.Core.NLogEx.Layout
{

    public class SalLogEventInfo
    {
        public DateTime TimeStamp { get; set; }
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }
        public Contour AdatpterContour { get; set; }
        public string AdapterHostName { get; set; }
        public string[] AdapterHostIp { get; set; }
        public string HandlerType { get; set; }
        public string HandlerName { get; set; }
        public string SessionId { get; set; }
        
        public string CorrelationId { get; set; } 
        
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }
        public string OperationId { get; set; } 
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public InternalExceptionDTO Exception { get; set; }
    }

    public class LogEventInfoProperty
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public LogEventInfoProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }


    [Layout("SalJsonLayout")]
    public class SalJsonLayout : NLog.Layouts.Layout
    {

        private static readonly JsonSerializerSettings jSettings;
        
        static SalJsonLayout()
        {
            jSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                TypeNameHandling = TypeNameHandling.None
            };

            jSettings.Converters.Add(new StringEnumConverter());
        }
        
        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {


            var salLogEvent = new SalLogEventInfo
            {
                TimeStamp = logEvent.TimeStamp,
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName,
                AdapterVersion = AdapterConfiguration.AdapterVersion,
                SalVersion = AdapterConfiguration.SalVersion,
                AdatpterContour = AdapterConfiguration.AdapterContour,
                HandlerType = HandlerContext.HandlerType.ToString(),
                HandlerName = HandlerContext.HandlerName,
                SessionId = HandlerContext.SessionId,
                CorrelationId = HandlerContext.CorrelationId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                OperationId = HandlerContext.OperationId,
                
                Level = logEvent.Level.Name,
                Logger = logEvent.LoggerName,
                Message = logEvent.FormattedMessage,
            };

            if (logEvent.Exception != null)
                salLogEvent.Exception = logEvent.Exception.ToDto();

            
            return JsonConvert.SerializeObject(salLogEvent, jSettings);
        }



    }
}
