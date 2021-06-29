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
            ContextInfo = new ContextInfo(src.ContextInfo);
        }

        public CommandDescriptor Descriptor { get; set; }
        
        public ContextInfo ContextInfo { get; set; }


    }
}