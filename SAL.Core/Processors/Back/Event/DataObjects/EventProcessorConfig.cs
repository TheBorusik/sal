namespace SAL.Core.Processors
{
    internal class EventProcessorConfig
    {
        public ushort PrefetchCount { get; set; } = 25;
        public ushort SystemPrefetchCount { get; set; } = 15;
    }
}