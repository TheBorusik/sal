using System;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface ISubscription : IDisposable
    {
        void Start();
        void Stop();
    }
}