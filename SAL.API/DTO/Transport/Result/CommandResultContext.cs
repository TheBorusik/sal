using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public record CommandResultContext
    {
        public CommandResultDescriptor Descriptor { get; set; }
        public ContextInfo ContextInfo { get; set; }
        public JObject Meta { get; init; }
    }
}