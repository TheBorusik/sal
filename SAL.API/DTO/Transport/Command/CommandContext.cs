namespace SAL.API
{
    public record CommandContext
    { 
        public CommandDescriptor Descriptor { get; init; }
        public ContextInfo ContextInfo { get; init; }
    }
}