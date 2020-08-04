using Autofac;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        private IWatchDog watchDog;
        protected bool IsOnline = false;

        protected void InitWatchDog()
        {
            watchDog = Container.Resolve<IWatchDog>();
            watchDog.Init(OnOnline, OnOffline);
        }

        protected void StartWatchDog()
        {
            watchDog.Start();
        }

        protected void StopWatchDog()
        {
            watchDog.Stop();
        }

        protected virtual void OnOnline()
        {
            if (!IsOnline)
            {
                logger.Info("Переход в Online");
                IsOnline = true;
                OnlineProcessors();
            }
        }
        protected virtual void OnOffline()
        {
            if (IsOnline)
            {
                logger.Info("Переход в Offline");
                IsOnline = false;
                OfflineProcessors();
            }
        }

        

    }
}