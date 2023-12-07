using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SAL.API;

namespace SAL.Core.Service
{
    internal partial class BackAdapter : IServiceProviderFactory<ContainerBuilder>, ISalService
    {
        private bool stoping = false;
        protected Logger logger;
        protected IContainer Container;

        public BackAdapter()
        {
            RemoveNewtonsoftJsonSchemaLicensing();
        }

        private void RemoveNewtonsoftJsonSchemaLicensing()
        {
            var type = Type.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseHelpers, Newtonsoft.Json.Schema");
            if (type == null) return;

            var method = type.GetMethod("SetRegisteredLicense", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) return;

            var ldType = Type.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseDetails, Newtonsoft.Json.Schema");
            if (ldType == null) return;

            var ldInstance = Activator.CreateInstance(ldType);
            var propInfo = ldType.GetProperty("Id");
            if (propInfo != null)
            {
                propInfo.SetValue(ldInstance, 10);
            }

            propInfo = ldType.GetProperty("ExpiryDate");
            if (propInfo != null)
            {
                propInfo.SetValue(ldInstance, DateTime.MaxValue);
            }

            propInfo = ldType.GetProperty("Type");
            if (propInfo != null)
            {
                propInfo.SetValue(ldInstance, "hacking");
            }

            method.Invoke(null, new[] { ldInstance });
        }

        public virtual void LoadConfiguration()
        {
            InitConfiguration();
            InitNLog();
        }

        public virtual void Initialization()
        {
            HandlerContext.Set(HandlerTypes.System, "Initialization");
            logger.Trace("Initializing...");
            ConfigureLimits();
            InitUnhandledExceptionHandler();
            InitWatchDog();
            InitProcessors();
            InitSystem();
            logger.Trace("Initialization complinted");
            ShowStartupInfo();
        }

        public void Start()
        {
            HandlerContext.Set(HandlerTypes.System, "Start");
            logger.Trace("Starting...");
            StartProcessors();
            StartTransport();
            logger.Trace("Base systems started.");
        }

        public void Stop()
        {
            HandlerContext.Set(HandlerTypes.System, "Stop");
            logger.Trace("Stopting...");
            stoping = true;
            StopProcessors();
            StopTransport();
            logger.Trace("Service stoped.");
        }

        protected virtual void InitUnhandledExceptionHandler()
        {
            logger.Trace("Init Unhandled Exception Handler");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = (Exception)args.ExceptionObject;
                Console.WriteLine($"UnhandledException: {ex.Message}\r{ex.StackTrace}");
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.Exception.Handle(exp =>
                {
                    if (exp is OperationCanceledException)
                        return true;

                    logger.Fatal(exp, $"TaskScheduler.UnobservedTaskException:\r\n");

                    return true;
                });
            };
        }

        protected virtual void ConfigureLimits()
        {
            logger.Trace($"ConfigureLimits");
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            ServicePointManager.ReusePort = true;
            ThreadPool.SetMaxThreads(2000, 1000);
            ThreadPool.SetMinThreads(100, 50);
        }

        protected virtual void ShowStartupInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine()
                .AppendLine("-------------------------------------------------------------")
                .AppendLine($"EnvUid          : {AdapterConfiguration.EnvUid}")
                .AppendLine($"InDocker        : {AdapterConfiguration.InDocker}")
                .AppendLine($"MachineName     : {AdapterConfiguration.MachineName}")
                .AppendLine($"AdapterContour  : {AdapterConfiguration.AdapterContour}")
                .AppendLine($"AdapterType     : {AdapterConfiguration.AdapterType}")
                .AppendLine($"AdapterName     : {AdapterConfiguration.AdapterName}")
                .AppendLine($"AdapterVersion  : {AdapterConfiguration.AdapterVersion}")
                .AppendLine($"ContourName     : {AdapterConfiguration.ContourName}")
                .AppendLine($"SalVersion      : {AdapterConfiguration.SalVersion} ({AdapterConfiguration.Revision})")
                .AppendLine($"RootPath        : {AdapterConfiguration.RootPath}")
                .AppendLine($"ConfigPath      : {AdapterConfiguration.ConfigPath}")
                .AppendLine($"LogRootPath     : {AdapterConfiguration.LogRootPath}")
                .AppendLine($"RootStorePath   : {AdapterConfiguration.DiskStorePath}")
                .AppendLine("-------------------------------------------------------------");
            logger.Info(sb.ToString());
        }
    }
}