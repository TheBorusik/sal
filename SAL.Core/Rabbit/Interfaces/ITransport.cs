using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Topology;
using SAL.Core.Transport;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface IRMQTransport : IMessageTransport
    {
        
        ILogger CreateLogger(string name);
        
        IModel CreateModel();
        void AddQueue(Queue queue);
        void AddExchange(Exchange queue);
        
    }
}