namespace SAL.API
{
    public record CommandResultContext
    {
        public CommandResultDescriptor Descriptor { get; set; }
        public ContextInfo ContextInfo { get; set; }
        
    }
}