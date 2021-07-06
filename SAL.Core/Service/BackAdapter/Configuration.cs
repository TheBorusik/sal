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
using SAL.Core.Configuration;
using SAL.Core.Configuration.Rabbit;
using SAL.Core.Exceptions;
using SAL.Core.Helpers;
using SAL.Core.NLogEx.Adapter;
using SAL.Core.NLogEx.Layout;
using SAL.Core.NLogEx.LayoutRenderer;
using SAL.Core.NLogEx.LayoutRenderer.HandlerContextRenderer;



namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        protected IConfigWatcher ConfigWatcher;
        protected NLogFactoryAdapter nLogFactory;
        public LogFactory LogFactory => nLogFactory.logFactory;
        public virtual void InitConfiguration()
        {
            var hostName = Environment.GetEnvironmentVariable("AdapterHostName");
            var dnsHostName = System.Net.Dns.GetHostName();
            AdapterConfiguration.AdapterHostName = string.IsNullOrWhiteSpace(hostName) ? dnsHostName : hostName;
            
            AdapterConfiguration.AdapterHostIp = System.Net.Dns.GetHostAddresses(dnsHostName).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(ip => ip.ToString()).ToArray();
            
            AdapterConfiguration.RootPath = AppDomain.CurrentDomain.BaseDirectory;
            AdapterConfiguration.ConfigPath = Path.Combine(AdapterConfiguration.RootPath, "config");
            if (!Directory.Exists(AdapterConfiguration.ConfigPath))
                Directory.CreateDirectory(AdapterConfiguration.ConfigPath);
            AdapterConfiguration.AdapterType = Environment.GetEnvironmentVariable("AdapterType");

            AdapterConfiguration.InDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            
            if(string.IsNullOrWhiteSpace(AdapterConfiguration.AdapterType))
                throw new ConfigurationErrorException("Не заданно значение AdapterType в переменных окружения");
            
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.Equals("SAL.Core", StringComparison.InvariantCultureIgnoreCase));
            if (assembly != null)
            {
                var version = assembly.GetName().Version;
                AdapterConfiguration.SalVersion = version.CalculateVersion();
                AdapterConfiguration.Revision = version.Build;
            }

            if(!bool.TryParse(Environment.GetEnvironmentVariable("UseLocalConfigs"), out var useLocalConfig))
                useLocalConfig = false;

            if (useLocalConfig)
            {
                var local = new ConfigWatcher();
                local.Init(AdapterConfiguration.ConfigPath);
                ConfigWatcher = local;
            }
            else
            {
                var remote = new RemoteConfigWatcher();
                remote.Init();
                ConfigWatcher = remote;
            }
            
            var firstConfig = ConfigWatcher.GetConfig();
            
            var modules = firstConfig.GetSafeValue<string[]>("Modules", null);
            if (modules != null && modules.Any())
            {
                var firstModule = modules.First();
                var firstModuleType = Type.GetType(firstModule, false);
                if (firstModuleType == null)
                    throw new ConfigurationErrorException("Не возможно получить тип первого модуля.");

                var mainAssembly = firstModuleType.Assembly;
                
                var fileVersionAttribute = mainAssembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                    .OfType<AssemblyFileVersionAttribute>().FirstOrDefault();
                
                if (string.IsNullOrWhiteSpace(fileVersionAttribute?.Version))
                {
                    AdapterConfiguration.AdapterVersion = "unknown";
                }
                else
                {
                    AdapterConfiguration.AdapterVersion = Regex.Replace(fileVersionAttribute?.Version, @"(\d+.\d+.\d+).*", "$1");
                }
            }
            
            var service = ConfigWatcher.GetSection(ConfigurationSectionNames.Service)?.ConvertValue<Configuration.Service>();
            if (service == null)
                throw new ConfigurationErrorException($"Не найдена секция {ConfigurationSectionNames.Service}");

            if (string.IsNullOrWhiteSpace(service.AdapterName))
            {
                throw new ConfigurationErrorException($"Не заданно значение {ConfigurationSectionNames.Service}.AdapterName");
            }

            if (!Regex.IsMatch(service.AdapterName, "^[A-z0-9-_#]+$"))
            {
                throw new ConfigurationErrorException($"AdapterName должен соответствовать регулярке '^[A-z0-9-_#]+$'");
            }

            AdapterConfiguration.AdapterName = service.AdapterName;

            if (string.IsNullOrWhiteSpace(service.LogRoot))
                service.LogRoot = "logs";

            AdapterConfiguration.LogRootPath = !Path.IsPathRooted(service.LogRoot)
                ? Path.Combine(AdapterConfiguration.RootPath, service.LogRoot)
                : service.LogRoot;
            
            if (string.IsNullOrWhiteSpace(service.RootStorePath))
                service.RootStorePath = "store";

            AdapterConfiguration.DiskStorePath = !Path.IsPathRooted(service.RootStorePath)
                ? Path.Combine(AdapterConfiguration.RootPath, service.RootStorePath, AdapterConfiguration.AdapterType)
                : service.RootStorePath;

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
            AdapterConfiguration.Contour = "BACK";
            
            var messageBus = ConfigWatcher.GetSection(ConfigurationSectionNames.MessageBus)?.ConvertValue<RabbitConfig>();
            if (messageBus == null)
            {
                throw new ConfigurationErrorException("Не найдена секция MessageBus");
            }
            
            AdapterConfiguration.ContourName = messageBus.VirtualHost.ToUpperInvariant();
        }

        protected virtual void InitNLog()
        {
            Layout.Register<SalJsonLayout>("SalJsonLayout");
            LayoutRenderer.Register<SessionIdLayoutRenderer>("sid");
            LayoutRenderer.Register<CorrelationIdLayoutRenderer>("cid");
            LayoutRenderer.Register<WfmProcessIdLayoutRenderer>("pid");
            LayoutRenderer.Register<AuthIdLayoutRenderer>("aid");
            LayoutRenderer.Register<OperationIdLayoutRenderer>("oid");
            
            LayoutRenderer.Register<SalMessageLayoutRenderer>("message");
            SalLayoutRenderRegistrar.Register(LayoutRenderer.Register);
            nLogFactory = new NLogFactoryAdapter(ConfigWatcher.GetSection(ConfigurationSectionNames.Nlog));
            logger = nLogFactory.GetLogger(nameof(BackAdapter));
        }

        public virtual void Done()
        {

        }
    }
}