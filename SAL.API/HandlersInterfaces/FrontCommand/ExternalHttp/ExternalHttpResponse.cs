using Newtonsoft.Json.Linq;

namespace SAL.API.FrontCommand
{
    public class ExternalHttpResponse
    {
        public int HttpCode { get; set; }
        public byte[] Body { get; set; }
        public JObject Headers { get; set; }
    }
}