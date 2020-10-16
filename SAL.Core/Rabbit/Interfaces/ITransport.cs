using System;
using SAL.Core.Rabbit.EventArgs;

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
}