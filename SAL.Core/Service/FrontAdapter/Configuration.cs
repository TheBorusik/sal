using System;
using System.IO;
using System.Text.RegularExpressions;
using NLog.LayoutRenderers;
using NLog.Layouts;
using SAL.API;
using SAL.Core.Configuration.Rabbit;
using SAL.Core.Exceptions;
using SAL.Core.NLogEx.Adapter;
using SAL.Core.NLogEx.Layout;
using SAL.Core.NLogEx.LayoutRenderer;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected override void ApplyConfiguration()
        {
            AdapterConfiguration.Contour = "FRONT";
            
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
            Layout.Register<SalJsonLayout>("SalJsonLayout");
            LayoutRenderer.Register<SidLayoutRenderer>("sid");
            LayoutRenderer.Register<SalMessageLayoutRenderer>("message");
            LayoutRenderer.Register<PidLayoutRenderer>("pid");
            SalLayoutRenderRegistrar.Register(LayoutRenderer.Register);
            nLogFactory = new NLogFactoryAdapter(ConfigWatcher.GetSection(ConfigurationSectionNames.Nlog));
            logger = nLogFactory.GetLogger(nameof(FrontAdapter));
        }
    }
}