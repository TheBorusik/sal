using System;
using System.Linq;
using Autofac;
using SAL.API;
using SAL.Core.Rabbit.EventArgs;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.WatchDog
{
    public class TransportMonitor : IWatchDogMonitor
    {
        public string MonitorName => "TransportMonitor";

        public event EventHandler<MonitorRestoreEventArgs> MonitorRestore;
        public event EventHandler<MonitorFailureEventArgs> MonitorFailure;

        private readonly ILifetimeScope container;
        private ITransport[] transports;

        public TransportMonitor(ILifetimeScope container)
        {
            this.container = container;
        }

        public bool Status { get; private set; }
        
        public void Init()
        {
            transports = container.Resolve<ITransport[]>();
        }

        private void TransportOnConnectionRestore(object sender, ConnectionRestoreEventArgs connectionRestoreEventArgs)
        {
            Status = transports.Aggregate(true, (b, transport) => b &= transport.IsConnected);
            MonitorRestore?.Invoke(this, new MonitorRestoreEventArgs
            {
                MonitorName = MonitorName
            });
        }

        private void TransportOnConnectionFailure(object sender, ConnectionFailureEventArgs connectionFailureEventArgs)
        {
            Status = transports.Aggregate(true, (b, transport) => b &= transport.IsConnected);
            MonitorFailure?.Invoke(this, new MonitorFailureEventArgs()
            {
                MonitorName = MonitorName
            });
        }

        public void Start()
        {
            foreach (var transport in transports)
            {
                transport.ConnectionFailure += TransportOnConnectionFailure;
                transport.ConnectionRestore += TransportOnConnectionRestore;
            }
            Status = transports.Aggregate(true, (b, transport) => b &= transport.IsConnected);
        }

        public void Stop()
        {
            foreach (var transport in transports)
            {
                transport.ConnectionFailure -= TransportOnConnectionFailure;
                transport.ConnectionRestore -= TransportOnConnectionRestore;
            }
            Status = transports.Aggregate(true, (b, transport) => b &= transport.IsConnected);
        }

        
    }
}
