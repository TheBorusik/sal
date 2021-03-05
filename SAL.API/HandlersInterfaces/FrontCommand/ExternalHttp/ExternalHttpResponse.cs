using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class ExternalHttpResponse
    {
        public int StatusCode { get; set; }
        public byte[] Body { get; set; }
        public JObject Headers { get; set; }
        public string ContentType { get; set; }
        public string RedirectLocation { get; set; }
        public bool? RedirectPermanent { get; set; }
    }
}