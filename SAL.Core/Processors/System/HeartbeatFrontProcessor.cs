using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.API.Const;
using SAL.Infrastructure;

namespace SAL.Core.Processors.System
{
    class HeartbeatFrontProcessor : HeartbeatBaseProcessor
    {
        private ILogger logger;
        private ISalClient salClient;

        public HeartbeatFrontProcessor(ILifetimeScope container, ILoggerProvider loggerProvider)
        {
            salClient = container.ResolveKeyed<ISalClient>(Contour.Front);
            logger = loggerProvider.CreateLogger(nameof(HeartbeatFrontProcessor));
        }

        protected override async Task Beat()
        {
            try
            {
                await salClient.PublishEventAsync("System.HeartbeatEvent", new HeartbeatEvent
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName,
                    AdapterContour = AdapterConfiguration.AdapterContour,
                    AdapterVersion = AdapterConfiguration.AdapterVersion,
                    SalVersion = AdapterConfiguration.SalVersion,
                    InDocker = AdapterConfiguration.InDocker,
                    MachineName = AdapterConfiguration.MachineName,
                    Timestamp = DateTime.UtcNow
                }, true, SalConst.SystemEventTTL);
            }
            catch (Exception ex)
            {
                logger.Error("При отправке HeartbeatEvent произошла ошибка", ex);
            }
        }
    }
}