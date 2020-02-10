namespace SAL.Core.Rabbit.Topology
{
    internal class QueueBinding
    {
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
    }
}