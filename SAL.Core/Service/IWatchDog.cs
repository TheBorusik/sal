using System;

namespace SAL.Core.Service
{
    public interface IWatchDog
    {
        void Init(Action onOnline, Action onOffline);
        void Start();
        void Stop();
    }
}