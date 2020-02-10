using System;

namespace SAL.Core.Exceptions.Rabbit
{
    public class MessageBusException : Exception
    {
    }

    public class NoConnectionException : Exception
    {
    }

    public class TopologyException : Exception
    {
    }

    public class DestinationQueueNotFoundException : Exception
    {
    }

    public class CreateExclusiveQueueException : Exception
    {
    }
}
