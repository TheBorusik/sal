using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Layouts;
using SAL.API;
using SAL.Core.NLogEx.LayoutRenderer;
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
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public LogExceptionDTO Exception { get; set; }
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
                Level = logEvent.Level.Name,
                Logger = logEvent.LoggerName,
                Message = logEvent.FormattedMessage.MaskSecretData(),
            };

            if (logEvent.Exception != null)
                salLogEvent.Exception = logEvent.Exception.ToLogDto();


            var jData = JObject.FromObject(salLogEvent, SalSerializer.JsonSerializer);
            var handlerData = HandlerContext.GetData();

            jData.Merge(handlerData);


            var a = JsonConvert.SerializeObject(jData,  Formatting.None, jSettings);
            return a;
        }



    }
}
