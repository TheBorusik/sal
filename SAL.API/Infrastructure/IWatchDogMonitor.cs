using System;

namespace SAL.API
{
    public interface IWatchDogMonitor
    {
        string MonitorName { get; }

        event EventHandler<MonitorRestoreEventArgs> MonitorRestore;
        event EventHandler<MonitorFailureEventArgs> MonitorFailure;

        bool Status { get; }

        void Init();
        void Start();
        void Stop();
    }
}