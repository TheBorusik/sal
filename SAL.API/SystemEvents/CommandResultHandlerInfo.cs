using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class CommandResultHandlerInfo
    {
        public string CommandName;
        public JSchema ResultSchema;
        public bool IsCommon;
    }
}