using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.SystemHandlers;

namespace SAL.Core.Processors.System
{
    class HeartbeatFrontProcessor : HeartbeatBaseProcessor
    {
        private ILogger logger;
        private ISalClient salClient;

        public HeartbeatFrontProcessor(ILifetimeScope container, ILoggerProvider loggerProvider)
        {
            salClient = container.ResolveNamed<ISalClient>("front");
            logger = loggerProvider.CreateLogger(nameof(HeartbeatFrontProcessor));
        }

        protected override async Task Beat()
        {
            try
            {
                await salClient.PublishEventAsync(new HeartbeatEvent
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName,
                    AdapterVersion = AdapterConfiguration.AdapterVersion,
                    SalVersion = AdapterConfiguration.SalVersion,
                    Timestamp = DateTime.UtcNow
                }, SystemEventTimes.BaseTTL);
            }
            catch (Exception ex)
            {
                logger.Error("При отправке HeartbeatEvent произошла ошибка", ex);
            }
        }
    }
}