namespace SAL.Core.Rabbit.Interfaces
{
    public class CommandInfo
    {
        public string CommandName { get; set; }
        public ushort PrefetchCount { get; set; }
    }
}