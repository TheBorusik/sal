using SAL.API;
using SAL.Core.Configuration.Rabbit;
using SAL.Core.Exceptions;
using SAL.Infrastructure;


namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected override void ApplyConfiguration()
        {
            AdapterConfiguration.AdapterContour = Contour.Front;
            
            var messageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.MessageBus)?.ConvertValue<RabbitConfig>();
            if (messageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция MessageBus");
            }

            AdapterConfiguration.BackContourName = messageBus.VirtualHost.ToUpperInvariant();

            var frontMessageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.FrontMessageBus)?.ConvertValue<RabbitConfig>();
            if (frontMessageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция FrontMessageBus");
            }
            
            AdapterConfiguration.ContourName = frontMessageBus.VirtualHost.ToUpperInvariant();

        }

        protected override void InitNLog()
        {
            base.InitNLog();
            logger = nLogFactory.GetLogger(nameof(FrontAdapter));
        }
    }
}