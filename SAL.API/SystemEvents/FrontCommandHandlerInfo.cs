using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class FrontCommandHandlerInfo
    {
        public string CommandName;
        public JSchema CommandSchema;
        public JSchema ResultSchema;
        public string[] ExternalUri;
    }
}