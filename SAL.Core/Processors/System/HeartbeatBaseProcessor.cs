using System;
using System.Threading;
using System.Threading.Tasks;
using SAL.API;
using SAL.API.Const;

namespace SAL.Core.Processors
{
    abstract class HeartbeatBaseProcessor : IProcessor
    {
        private CancellationTokenSource cancellationToken;
        private Task beatTask = Task.CompletedTask;

        public void Init() { }
        public void Start()
        {
        }

        public void Online()
        {
            if (beatTask.Status == TaskStatus.RanToCompletion)
            {
                cancellationToken = new CancellationTokenSource();
                beatTask = Task.Run(async () =>
                {
                    var sendEvent = new AutoResetEvent(false);
                    var beatTimer = new Timer(s => sendEvent.Set(), null, TimeSpan.FromSeconds(0), SalConst.HearBeatInterval);
                    var waitHandles = new[] { cancellationToken.Token.WaitHandle, sendEvent };

                    while (true)
                    {
                        WaitHandle.WaitAny(waitHandles);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await Beat();
                    }

                    beatTimer.Dispose();
                });
            }
        }

        protected abstract Task Beat();


        public void Offline()
        {
            cancellationToken.Cancel();
        }

        public void Stop()
        {
            cancellationToken.Cancel();
            beatTask.Wait();
        }
    }
}