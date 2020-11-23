namespace SAL.Core.Processors
{
    internal class CommandResultProcessorConfig
    {
        public ushort GlobalPrefetchCount { get; set; } = 25;
        public ushort InstancePrefetchCount { get; set; } = 15;
        public ushort TypePrefetchCount { get; set; } = 5;
        public ushort SyncPrefetchCount { get; set; } = 5;

    }
}