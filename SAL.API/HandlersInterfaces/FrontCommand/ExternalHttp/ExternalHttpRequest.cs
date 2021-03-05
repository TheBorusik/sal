using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class ExternalHttpRequest
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; }
        public JObject Headers { get; set; }
        
        public string QueryString { get; set; }
        public JObject QueryData { get; set; }




        public bool HasFormData { get; set; } 
        public JObject FormData { get; set; }
        public byte[] RawData { get; set; }
        public ConnectionInfo ConnectionInfo { get; set; }

    }

    
    




}