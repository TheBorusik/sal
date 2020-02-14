using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class CommandResultContext
    {
        public CommandResultDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }
        public JObject CallTrace { get; set; }

    }
}