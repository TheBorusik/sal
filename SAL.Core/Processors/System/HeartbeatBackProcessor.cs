using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.API.Const;


namespace SAL.Core.Processors
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
                await salClient.PublishEventAsync("System.HeartbeatEvent",new HeartbeatEvent
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName,
                    AdapterContour = AdapterConfiguration.AdapterContour,
                    AdapterVersion = AdapterConfiguration.AdapterVersion,
                    SalVersion = AdapterConfiguration.SalVersion,
                    InDocker = AdapterConfiguration.InDocker,
                    MachineName = AdapterConfiguration.MachineName,
                    
                    Timestamp = DateTime.UtcNow
                },  SalConst.SystemEventTTL);
            }
            catch (Exception ex)
            {
                logger.Error("При отправке HeartbeatEvent произошла ошибка", ex);
            }
        }
    }
}
