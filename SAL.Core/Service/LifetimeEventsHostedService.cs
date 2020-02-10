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
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            logger.LogInformation("OnStarted has been called.");
            salService.Start();

            // Perform post-startup activities here
        }

        private void OnStopping()
        {
            logger.LogInformation("OnStopping has been called.");
            salService.Stop();
            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
    
}