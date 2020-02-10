namespace SAL.Core.Rabbit.Topology
{
    internal class Exchange
    {
        public string Name { get; set; }
        public ExchangeType Type { get; set; }
        public bool Durable { get; set; } = true;
        public string AlternateExchange { get; set; }
    }
}