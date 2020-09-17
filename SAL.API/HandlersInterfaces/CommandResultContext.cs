using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public class CommandResultContext
    {
        public CommandResultContext()
        {

        }
        public CommandResultContext(CommandResultContext src)
        {
            Descriptor = new CommandResultDescriptor(src.Descriptor);
            Session = Session.Clone();
            CallTrace = CallTrace.Clone();
        }

        public CommandResultDescriptor Descriptor { get; set; }
        public JObject Session { get; set; }
        public JObject CallTrace { get; set; }

    }
}