using System;

namespace SAL.Core.Rabbit.Topology
{
    internal class Queue
    {
        public string Name { get; set; }

        public int MaxPriority { get; set; }
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }
        public bool Durable { get; set; }

        public TimeSpan? Expire { get; set; }
        public bool HasDeadLetter { get; set; }
        public string DeadLetterRoutingKey { get; set; }

        public QueueBinding[] Bindings { get; set; }
    }
}