using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Layouts;
using SAL.API;

namespace SAL.Core.NLogEx.Layout
{

    public class SalLogEventInfo
    {
        public DateTime TimeStamp { get; set; }
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public string AdapterVersion { get; set; }
        public int SalVersion { get; set; }
        public string Contour { get; set; }
        public string AdapterHostName { get; set; }
        public string[] AdapterHostIp { get; set; }
        public string HandlerType { get; set; }
        public string HandlerName { get; set; }
        public string SessionId { get; set; }
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

        private static readonly JsonSerializerSettings JSettings;
        private static readonly JsonSerializer JSerializer;

        static SalJsonLayout()
        {
            JSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                TypeNameHandling = TypeNameHandling.None
            };

            JSettings.Converters.Add(new StringEnumConverter());

            JSerializer = JsonSerializer.CreateDefault(JSettings);
        }

        public bool UniversalTime { get; set; }

        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            var jObject = new JObject();
            logEvent.Parameters?.ForEach(p =>
            {
                if (p is LogEventInfoProperty leiProperty)
                {
                    jObject.Add(ToCamelCase(leiProperty.Name), JToken.FromObject(leiProperty.Value, JSerializer));
                }
            });

            var salLogEvent = new SalLogEventInfo
            {
                TimeStamp = logEvent.TimeStamp,
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName,
                AdapterVersion = AdapterConfiguration.AdapterVersion,
                SalVersion = AdapterConfiguration.SalVersion,
                Contour = AdapterConfiguration.Contour,
                AdapterHostIp = AdapterConfiguration.AdapterHostIp,
                AdapterHostName = AdapterConfiguration.AdapterHostName,
                HandlerType = HandlerContext.Type,
                HandlerName = HandlerContext.Name,
                Level = logEvent.Level.Name,
                Logger = logEvent.LoggerName,
                Message = logEvent.FormattedMessage,
            };

            if (logEvent.Exception != null)
                salLogEvent.Exception = logEvent.Exception.ToDto();

            if (SessionManager.Current != null)
            {
                salLogEvent.SessionId = SessionManager.Current.GetSafeValue(SessionNames.SessionId, "");
            }

            jObject.Merge(JObject.FromObject(salLogEvent, JSerializer));


            return JsonConvert.SerializeObject(jObject, JSettings);
        }


        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            {
                return s;
            }

            var chars = s.ToCharArray();

            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                var hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    if (char.IsSeparator(chars[i + 1]))
                    {
                        chars[i] = ToLower(chars[i]);
                    }

                    break;
                }

                chars[i] = ToLower(chars[i]);
            }

            return new string(chars);
        }

        private static char ToLower(char c)
        {
            c = char.ToLowerInvariant(c);
            return c;
        }
    }
}
