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
    public partial class BackAdapter
    {
        protected IConfigWatcher ConfigWatcher;
        protected NLogFactoryAdapter nLogFactory;
        public LogFactory LogFactory => nLogFactory.logFactory;
        public virtual void InitConfiguration()
        {

            ServiceConfiguration.AdapterHostName = System.Net.Dns.GetHostName();
            ServiceConfiguration.AdapterHostIp = System.Net.Dns.GetHostAddresses(ServiceConfiguration.AdapterHostName).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(ip => ip.ToString()).ToArray();


            ServiceConfiguration.RootPath = AppDomain.CurrentDomain.BaseDirectory;
            ServiceConfiguration.ConfigPath = Path.Combine(ServiceConfiguration.RootPath, "config");


            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.Equals("TCB.SAL.Core", StringComparison.InvariantCultureIgnoreCase));
            if (assembly != null)
            {
                var version = assembly.GetName().Version;
                ServiceConfiguration.SalVersion = version.CalculateVersion();
                ServiceConfiguration.Revision = version.Revision;
            }
            var configWatcher = new ConfigWatcher();

            var firstConfig = configWatcher.Init(ServiceConfiguration.ConfigPath);
            var modules = firstConfig.GetSafeValue<string[]>("Modules", null);
            if (modules != null && modules.Any())
            {
                var firstModule = modules.First();
                var firstModuleType = Type.GetType(firstModule, false);
                if (firstModuleType == null)
                    throw new ConfigurationErrorException("Не возможно получить тип первого модуля.");

                var mainAssembly = firstModuleType.Assembly;
                var ssAttribute = mainAssembly.GetCustomAttributes(typeof(SalServiceTypeAttribute))
                    .OfType<SalServiceTypeAttribute>().FirstOrDefault();
                if (string.IsNullOrWhiteSpace(ssAttribute?.Type))
                {
                    throw new ConfigurationErrorException("Не заданно значение SalServiceTypeAttribute в главной сборке");
                }
                ServiceConfiguration.AdapterType = ssAttribute.Type;

                var fileVersionAttribute = mainAssembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                    .OfType<AssemblyFileVersionAttribute>().FirstOrDefault();


                ServiceConfiguration.AdapterVersion = fileVersionAttribute?.Version ?? "unknown";


            }
            else
            {
                throw new ConfigurationErrorException("Не заданно значение Modules в конфиге");
            }



            ConfigWatcher = configWatcher;


            ApplyConfiguration();

            ConfigWatcher.Subscribe(ConfigurationSectionNames.Nlog, NlogConfigChanged);
        }

        protected void NlogConfigChanged(object sender, ConfigurationSectionChangedEventArgs args)
        {
            if (!args.SectionName.Equals(ConfigurationSectionNames.Nlog, StringComparison.InvariantCultureIgnoreCase)) return;
            nLogFactory.Reload(ConfigWatcher.GetSection(ConfigurationSectionNames.Nlog));
        }

        protected virtual void ApplyConfiguration()
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


        }

        protected virtual void InitNLog()
        {
            Layout.Register<SalJsonLayout>("SalJsonLayout");
            LayoutRenderer.Register<SidLayoutRenderer>("sid");
            nLogFactory = new NLogFactoryAdapter(ConfigWatcher.GetSection(ConfigurationSectionNames.Nlog));
            logger = nLogFactory.GetLogger(nameof(BackAdapter));
        }


    }
}