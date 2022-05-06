using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class EventHandlerInfo
    {
        public string EventName;
        public JSchema EventSchema;
    //    public bool IsCommon;
    }
}