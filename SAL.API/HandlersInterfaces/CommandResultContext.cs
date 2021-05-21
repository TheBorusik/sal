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
            SessionId = src.SessionId;
            AuthId = src.AuthId;
            ProcessId = src.ProcessId;
            OperationId = src.OperationId;
        }
        
        public CommandResultContext Clone()
        {
            return new CommandResultContext(this);
        }

        public CommandResultDescriptor Descriptor { get; set; }

        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }
        public string OperationId { get; set; }
    }
}