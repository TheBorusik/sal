using System;

namespace SAL.Core.Rabbit.Topology
{
    public class Queue
    {
        public string Name { get; set; }

        public int MaxPriority { get; set; }
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }
        public bool Durable { get; set; }

        public TimeSpan? Expire { get; set; }
        public bool HasDeadLetter { get; set; }
        public string DeadLetterExchange { get; set; }

        public Binding[] Bindings { get; set; }
    }
}