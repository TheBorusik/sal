using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Topology;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface ITransport : IDisposable
    {
        void Start();
        void Stop();

        event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;
        event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;

        ISubscriptionFactory CreateMessageSubscription();
        IPublisher CreatePublisher();
        

        bool IsConnected { get; }
        
        string CounterName { get; }


    }

    public interface IRMQTransport
    {
        ILogger CreateLogger(string name);
        
        IModel CreateModel();
        void AddQueue(Queue queue);
    }
}