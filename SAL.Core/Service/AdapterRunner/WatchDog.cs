using Autofac;

namespace SAL.Core.Service
{
    public partial class AdapterRunner
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

        protected void OnOnline()
        {
            if (!IsOnline)
            {
                IsOnline = true;
                OnlineProcessors();
            }
        }
        protected void OnOffline()
        {
            if (IsOnline)
            {
                IsOnline = false;
                OfflineProcessors();
            }
        }

        

    }
}