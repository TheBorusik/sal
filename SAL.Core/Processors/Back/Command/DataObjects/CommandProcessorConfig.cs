using System.Collections.Generic;

namespace SAL.Core.Processors
{
    internal class CommandProcessorConfig
    {
        public ushort GlobalPrefetchCount { get; set; }
        public ushort CommandPrefetchCount { get; set; }
        public Dictionary<string, CommandProcessingSettings> CommandProcessingSettings { get; set; }
    }
}