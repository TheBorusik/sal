namespace SAL.API
{
    public class EventHandlerInfo
    {
        public string EventName;
        public string EventDto;
        public bool IsSystem;
        public bool IsCommon;

        public DtoInfo[] Dtos { get; set; }
    }
}