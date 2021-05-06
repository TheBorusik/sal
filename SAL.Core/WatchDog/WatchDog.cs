using System;
using System.Linq;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.Service;

namespace SAL.Core.WatchDog
{
    public class WatchDog : IWatchDog
    {
        private readonly ILifetimeScope container;
        private readonly ILogger<WatchDog> logger;
        private IWatchDogMonitor[] monitors;



        private Action OnOnline;
        private Action OnOffline;

        private bool AggStatus;


        public WatchDog(ILifetimeScope container, ILogger<WatchDog> logger)
        {
            this.container = container;
            this.logger = logger;
            monitors = container.Resolve<IWatchDogMonitor[]>();
        }


        public void Init(Action onOnline, Action onOffline)
        {
            OnOnline = onOnline;
            OnOffline = onOffline;
            HandlerContext.Set(HandlerTypes.WatchDog, "Init");
            
            monitors.ForEach(m =>
            {
                m.Init();
                m.MonitorFailure += OnMonitorFailure;
                m.MonitorRestore += OnMonitorRestore;
            });
        }

        private void OnMonitorRestore(object sender, MonitorRestoreEventArgs monitorRestoreEventArgs)
        {
            if (AggStatus)
                return;
            CheckStatus();

        }

        private void OnMonitorFailure(object sender, MonitorFailureEventArgs monitorFailureEventArgs)
        {
            if (!AggStatus)
                return;
            CheckStatus();
        }

        public void Start()
        {
            monitors.ForEach(m => m.Start());
            CheckStatus();

        }

        public void Stop()
        {
            monitors.ForEach(m =>
            {
                m.Stop();
                m.MonitorFailure -= OnMonitorFailure;
                m.MonitorRestore -= OnMonitorRestore;
            });
            AggStatus = false;
            OnOffline?.Invoke();
        }


        private void CheckStatus()
        {

            AggStatus = monitors.Aggregate(true, (s, m) => s &= m.Status);
            if (AggStatus)
                OnOnline?.Invoke();
            else
                OnOffline?.Invoke();

        }


    }
}
