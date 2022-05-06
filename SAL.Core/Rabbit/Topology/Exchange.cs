using System;

namespace SAL.Core.Rabbit.Topology
{
    public class Exchange
    {
        public string Name { get; set; }
        public ExchangeType Type { get; set; }
        public bool Durable { get; set; } = true;
        public string AlternateExchange { get; set; }

        public Binding[] Bindings { get; set; } = Array.Empty<Binding>();
    }
}