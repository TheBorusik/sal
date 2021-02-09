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

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger,
            IHostApplicationLifetime appLifetime,
            ISalService salService)
        {
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.salService = salService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            salService.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                salService.Stop();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error on stop");
            }

            return Task.CompletedTask;
        }
    }
}