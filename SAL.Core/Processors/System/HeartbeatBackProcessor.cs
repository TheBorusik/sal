using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.API.Client;
using SAL.API.LoggerHelper;
using SAL.Core.SystemEventHandlers;

namespace SAL.Core.Processors.System
{
    class HeartbeatBackProcessor : HeartbeatBaseProcessor
    {
        private ILogger logger;
        private ISalClient salClient;

        public HeartbeatBackProcessor(ILifetimeScope container, ILoggerProvider loggerProvider)
        {
            salClient = container.Resolve<ISalClient>();
            logger = loggerProvider.CreateLogger(nameof(HeartbeatBackProcessor));
        }

        protected override async Task Beat()
        {
            try
            {
                await salClient.PublishEventAsync(new HeartbeatEvent
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName,
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
