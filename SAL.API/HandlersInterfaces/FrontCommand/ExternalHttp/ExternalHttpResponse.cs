using Newtonsoft.Json.Linq;

namespace SAL.API.FrontCommand
{
    public class ExternalHttpResponse
    {
        public int StatusCode { get; set; }
        public byte[] Body { get; set; }
        public JObject Headers { get; set; }
        public string ContentType { get; set; }
    }
}