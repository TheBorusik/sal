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

        private object onlineLocker = new object();

        protected virtual void OnOnline()
        {
            lock (onlineLocker)
            {
                if (!IsOnline && !stoping)
                {
                    logger.Info("Переход в Online");
                    IsOnline = true;
                    SendOnline();
                    OnlineProcessors();
                }
            }
        }
        protected virtual void OnOffline()
        {
            lock (onlineLocker)
            {
                if (IsOnline)
                {
                    logger.Info("Переход в Offline");
                    IsOnline = false;
                    OfflineProcessors();
                    SendOffline();
                }
            }
        }

        

    }
}