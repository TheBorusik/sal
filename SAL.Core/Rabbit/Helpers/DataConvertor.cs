using System;
using RabbitMQ.Client;

namespace SAL.Core.Rabbit.Helpers
{
    internal static class DataConvertor
    {
        public static DateTime ToDateTime(this AmqpTimestamp timestamp)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(timestamp.UnixTime);
        }

        public static AmqpTimestamp ToAmqp(this DateTime timestamp)
        {
            return new AmqpTimestamp((long)timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public static string ToRMQ(this Topology.ExchangeType exchangeType)
        {
            switch (exchangeType)
            {
                case Topology.ExchangeType.Direct:
                    return ExchangeType.Direct;
                case Topology.ExchangeType.Fanout:
                    return ExchangeType.Fanout;
                case Topology.ExchangeType.Topic:
                    return ExchangeType.Topic;
            }
            return ExchangeType.Direct;
        }
    }
}
