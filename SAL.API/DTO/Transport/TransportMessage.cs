using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class TransportMessage
    {
        public string Type { get; set; }
        public JObject Payload { get; set; }
    }
}
