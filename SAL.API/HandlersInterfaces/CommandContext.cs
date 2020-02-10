using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class CommandContext
    {
        public CommandDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }
        public JObject CallTrace { get; set; }

    }
}