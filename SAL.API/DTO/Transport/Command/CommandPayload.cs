using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public record CommandPayload
    {
        public CommandContext Context { get; init; }
        public JObject Payload { get; init; }
    }
}