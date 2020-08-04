using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;
using NLog.LayoutRenderers;
using NLog.Layouts;
using SAL.API;
using SAL.Core.Config;
using SAL.Core.Config.Rabbit;
using SAL.Core.Exceptions;
using SAL.Core.Helpers;
using SAL.Core.NLogEx.Adapter;
using SAL.Core.NLogEx.Layout;
using SAL.Core.NLogEx.LayoutRenderer;
using SAL.Infrastructure;

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

            ServiceConfiguration.AdapterName = service.AdapterName;

            if (string.IsNullOrWhiteSpace(service.LogRoot))
                service.LogRoot = "c:\\.Logs";

            ServiceConfiguration.LogRootPath = !Path.IsPathRooted(service.LogRoot)
                ? Path.Combine(ServiceConfiguration.RootPath, service.LogRoot)
                : service.LogRoot;

            if (string.IsNullOrWhiteSpace(service.DataPath))
                service.DataPath = "Dto";


            if (string.IsNullOrWhiteSpace(service.DiskStorePath))
                service.DiskStorePath = "Store";

            ServiceConfiguration.DiskStorePath = !Path.IsPathRooted(service.DiskStorePath)
                ? Path.Combine(ServiceConfiguration.RootPath, service.DiskStorePath)
                : service.DiskStorePath;

            if (!Directory.Exists(ServiceConfiguration.DiskStorePath))
                Directory.CreateDirectory(ServiceConfiguration.DiskStorePath);

            try
            {
                if (!Directory.Exists(ServiceConfiguration.DiskStorePath))
                    Directory.CreateDirectory(ServiceConfiguration.DiskStorePath);


                var fn = Path.Combine(ServiceConfiguration.DiskStorePath, Guid.NewGuid().ToString());
                File.WriteAllText(fn, "Тест");
                File.Delete(fn);
            }
            catch (Exception)
            {
                throw new ConfigurationErrorException($"DiskStore - Ошибка конфигурации. Проверте  доступность \"{ServiceConfiguration.DiskStorePath}\".");
            }

            var messageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.MessageBus)?.ConvertValue<RabbitConfig>();
            if (messageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция MessageBus");
            }

            ServiceConfiguration.Contour = messageBus.VirtualHost.ToUpperInvariant();

            var frontMessageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.FrontMessageBus)?.ConvertValue<RabbitConfig>();
            if (frontMessageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция FrontMessageBus");
            }
            
            ServiceConfiguration.FrontContour = frontMessageBus.VirtualHost.ToUpperInvariant();

        }
    }
}