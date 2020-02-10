using System.Collections.Generic;

namespace SAL.Core.Config.Logging
{
    class LoggingConfig
    {
        public Dictionary<string, LoggingItem> Items { get; set; } 
    }

    public class LoggingItem
    {
        public bool Ignore { get; set; } = false;
        public bool Formatting { get; set; } = false;
        public int СropSize { get; set; } = -1;
    }


}
