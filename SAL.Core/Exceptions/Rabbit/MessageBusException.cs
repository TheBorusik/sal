using System;

namespace SAL.Core.Exceptions.Rabbit
{
    public class MessageBusException : Exception
    {
        public MessageBusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class MessageNotPublishedException : Exception
    {
        public MessageNotPublishedException(string message) : base(message)
        {
        }
    }
    public class MessagePublishedNotConfirmedException : Exception
    {
        
    }
    

    public class NoConnectionException : Exception
    {
    }

    
    public class MessageIsTooLongException : Exception
    {
    }
    
    public class TopologyException : Exception
    {
    }



    public class CreateExclusiveQueueException : Exception
    {
    }
}
