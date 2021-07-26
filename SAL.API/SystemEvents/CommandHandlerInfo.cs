using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class CommandHandlerInfo
    {
        public string CommandName;
        public bool IsCommon;
        public bool IsInstanceHandler;
        public JSchema CommandSchema;
        public JSchema ResultSchema;
    }
}