using System;
using System.IO;
using System.Text.RegularExpressions;
using NLog.LayoutRenderers;
using NLog.Layouts;
using SAL.API;
using SAL.Core.Config;
using SAL.Core.Config.Rabbit;
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
            var service = ConfigWatcher.GetSection(ConfigurationSectionNames.Service)?.ConvertValue<Config.Service.Service>();
            if (service == null)
                throw new ConfigurationErrorException($"Не найдена секция {ConfigurationSectionNames.Service}");

            if (string.IsNullOrWhiteSpace(service.AdapterName))
            {
                throw new ConfigurationErrorException($"Не заданно значение {ConfigurationSectionNames.Service}.AdapterName");
            }

            if (!Regex.IsMatch(service.AdapterName, "^[A-z0-9]+$"))
            {
                throw new ConfigurationErrorException($"AdapterName должен содержать только буквы или цифры (^[A-z0-9]+$)");
            }

            AdapterConfiguration.AdapterName = service.AdapterName;

            if (string.IsNullOrWhiteSpace(service.LogRoot))
                service.LogRoot = "c:\\.Logs";

            AdapterConfiguration.LogRootPath = !Path.IsPathRooted(service.LogRoot)
                ? Path.Combine(AdapterConfiguration.RootPath, service.LogRoot)
                : service.LogRoot;

            if (string.IsNullOrWhiteSpace(service.DataPath))
                service.DataPath = "Dto";


            if (string.IsNullOrWhiteSpace(service.DiskStorePath))
                service.DiskStorePath = "Store";

            AdapterConfiguration.DiskStorePath = !Path.IsPathRooted(service.DiskStorePath)
                ? Path.Combine(AdapterConfiguration.RootPath, service.DiskStorePath)
                : service.DiskStorePath;

            if (!Directory.Exists(AdapterConfiguration.DiskStorePath))
                Directory.CreateDirectory(AdapterConfiguration.DiskStorePath);

            try
            {
                if (!Directory.Exists(AdapterConfiguration.DiskStorePath))
                    Directory.CreateDirectory(AdapterConfiguration.DiskStorePath);


                var fn = Path.Combine(AdapterConfiguration.DiskStorePath, Guid.NewGuid().ToString());
                File.WriteAllText(fn, "Тест");
                File.Delete(fn);
            }
            catch (Exception)
            {
                throw new ConfigurationErrorException($"DiskStore - Ошибка конфигурации. Проверте  доступность \"{AdapterConfiguration.DiskStorePath}\".");
            }

            var messageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.MessageBus)?.ConvertValue<RabbitConfig>();
            if (messageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция MessageBus");
            }

            AdapterConfiguration.BackContour = messageBus.VirtualHost.ToUpperInvariant();

            var frontMessageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.FrontMessageBus)?.ConvertValue<RabbitConfig>();
            if (frontMessageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция FrontMessageBus");
            }
            
            AdapterConfiguration.Contour = frontMessageBus.VirtualHost.ToUpperInvariant();

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