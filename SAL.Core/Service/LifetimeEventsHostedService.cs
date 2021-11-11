using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SAL.Core.Service
{
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger logger;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ISalService salService;
        private IWatchDog watchDog;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger,
            IHostApplicationLifetime appLifetime,
            ISalService salService, 
            IWatchDog watchDog)
        {
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.salService = salService;
            this.watchDog = watchDog;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            watchDog.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            watchDog.Stop();
            salService.Stop();
            return Task.CompletedTask;
        }
    }
}