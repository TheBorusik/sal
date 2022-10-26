using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class FrontCommandHandlerInfo
    {
        public string CommandName;
        public string Version;
        public JSchema CommandSchema;
        public JSchema ResultSchema;
    }
}