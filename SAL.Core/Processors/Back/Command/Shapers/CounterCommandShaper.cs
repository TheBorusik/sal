using Newtonsoft.Json.Linq;

namespace SAL.Core.Processors.Shapers
{

    public class CounterCommandShaperConfig
    {
        public int Count { get; set; }
        public int Interval { get; set; }
    }

    public class CounterCommandShaper : ICommandShaper
    {
        private readonly string commandName;
        private CounterCommandShaperConfig config;
        private LiteDB.LiteDatabase dateDatabase;

        public CounterCommandShaper(string commandName, JToken config, LiteDB.LiteDatabase dateDatabase)
        {
            this.commandName = commandName;
            this.config = config.ToObject<CounterCommandShaperConfig>();
            this.dateDatabase = dateDatabase;
        }


    }
}