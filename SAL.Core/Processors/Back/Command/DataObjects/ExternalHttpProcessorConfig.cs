using System.Collections.Generic;

namespace SAL.Core.Processors
{
    internal class ExternalHttpProcessorConfig
    {
        public ushort GlobalPrefetchCount { get; set; }
        public Dictionary<string, CommandProcessingSettings> ExternalHttpSettings { get; set; }
    }
}