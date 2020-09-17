using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class CommandContext
    {
        public CommandContext()
        {

        }

        public CommandContext(CommandContext src)
        {
            Descriptor = new CommandDescriptor(src.Descriptor);
            Session = src.Session.Clone();
            CallTrace = src.CallTrace.Clone();
        }

        public CommandDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }
        public JObject CallTrace { get; set; }

    }
}