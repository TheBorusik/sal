using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;

namespace SAL.API.Events
{
    [SalSystemEvent]
    [SalServiceType("System")]
    [SalEventName("IAmBack")]
    public class IAmBackEvent : IEvent
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public CommandHandlerInfo[] CommandHandlers { get; set; }
        public CommandResultHandlerInfo[] CommandResultHandlers { get; set; }
        public EventHandlerInfo[] EventHandlers { get; set; }

        public DtoInfo[] Dto { get; set; }
    }


    public class CommandHandlerInfo
    {
        public string CommandName;
        public bool IsCommon;
        public bool IsInstanceHandler;
        public string CommandDto;
        public string ResultDto;
    }

    public class FrontCommandHandlerInfo : CommandHandlerInfo
    {

    }

    public class CommandResultHandlerInfo
    {
        public string CommandName;
        public string CommandDto;
        public string ResultDto;
        public bool IsCommon;
    }

    public class EventHandlerInfo
    {
        public string EventName;
        public string EventDto;
        public bool IsSystem;
        public bool IsCommon;
    }

    public class DtoInfo
    {
        public string Name { get; set; }

        public FieldInfo[] FieldsInfos { get; set; }
    }

    public class FieldInfo
    {
        public int Order { get; set; }
        public bool IsRequired { get; set; }
        public string Name { get; set; }
        public FieldType Type { get; set; }
        public string ObjectName { get; set; }

        public FieldType ElementType { get; set; }
        public string ElementObjectName { get; set; }
    }

    public enum FieldType
    {
        None = 0,
        Object = 1,
        Array = 2,
        Integer =3,
        Float = 4,
        String = 5,
        Boolean = 6,
        Bytes = 7,
        Guid = 8,
        Date = 9,
        TimeSpan= 10

    }
}