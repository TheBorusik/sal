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
            SessionId = src.SessionId;
            AuthId = src.AuthId;
            ProcessId = src.ProcessId;

        }

        public CommandDescriptor Descriptor { get; set; }
        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }

    }
}