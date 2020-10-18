using System;
using Autofac;
using SAL.API;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.WatchDog
{
    public class BackTransportMonitor : IWatchDogMonitor
    {
        public string MonitorName => "BackTransportMonitor";

        public event EventHandler<MonitorRestoreEventArgs> MonitorRestore;
        public event EventHandler<MonitorFailureEventArgs> MonitorFailure;

        private readonly ILifetimeScope container;
        private ITransport transport;

        public BackTransportMonitor(ILifetimeScope container)
        {
            this.container = container;
        }

        public bool Status => transport.IsConnected;

        public void Init()
        {
            transport = container.Resolve<ITransport>();

        }

        private void TransportOnConnectionRestore(object sender, ConnectionRestoreEventArgs connectionRestoreEventArgs)
        {
            MonitorRestore?.Invoke(this, new MonitorRestoreEventArgs
            {
                MonitorName = MonitorName
            });
        }

        private void TransportOnConnectionFailure(object sender, ConnectionFailureEventArgs connectionFailureEventArgs)
        {
            MonitorFailure?.Invoke(this, new MonitorFailureEventArgs()
            {
                MonitorName = MonitorName
            });
        }

        public void Start()
        {
            transport.ConnectionFailure += TransportOnConnectionFailure;
            transport.ConnectionRestore += TransportOnConnectionRestore;
        }

        public void Stop()
        {
            transport.ConnectionFailure -= TransportOnConnectionFailure;
            transport.ConnectionRestore -= TransportOnConnectionRestore;
        }
    }
}
