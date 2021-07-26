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
            ContextInfo = new ContextInfo(src.ContextInfo);
        }
        
        public CommandResultContext(CommandContext src)
        {
            Descriptor = new CommandResultDescriptor(src.Descriptor);
            ContextInfo = new ContextInfo(src.ContextInfo);

        }
        
        public CommandResultContext Clone()
        {
            return new CommandResultContext(this);
        }

        public CommandResultDescriptor Descriptor { get; set; }
        public ContextInfo ContextInfo { get; set; }
        
    }
}