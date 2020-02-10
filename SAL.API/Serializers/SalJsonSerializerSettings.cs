using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace SAL.API
{
    public class SalJsonSerializerSettings
    {
        public JsonSerializerSettings SerializerSettings { get; }

        public JsonLoadSettings LoadSettings { get; set; }

        public SalJsonSerializerSettings(bool indented = false)
        {
            SerializerSettings = new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented :Formatting.None,
                ContractResolver = new DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                TypeNameHandling = TypeNameHandling.None
            };

            SerializerSettings.Converters.Add(new StringEnumConverter());


            LoadSettings = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Ignore
            };
        }

    }

}
