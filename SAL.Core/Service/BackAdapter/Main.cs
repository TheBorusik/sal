using System;
using System.Net;
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
        protected Logger logger;
        protected IContainer Container;


        public virtual void LoadConfiguration()
        {
            InitConfiguration();
            InitNLog();
        }


        public virtual void Initialization()
        {
            SessionManager.SetNewSession($"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}#Init#");
            logger.Trace("Инициализация...");
            ConfigureLimits();
            InitUnhandledExceptionHandler();
            InitWatchDog();
            InitProcessors();
            InitSystem();
            logger.Trace("Инициализация завершена");
            ShowStartupInfo();
        }


        public void Start()
        {
            SessionManager.SetNewSession($"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}#Start#");
            logger.Trace("Запуск...");
            StartWatchDog();
            StartProcessors();
            StartTransport();
            logger.Trace("Основные системы запущены.");
        }


        private bool stoping = false;

        public void Stop()
        {
            SessionManager.SetNewSession($"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}#Stop#");
            logger.Trace("Остановка...");
            stoping = true;
            StopWatchDog();
            StopProcessors();
            StopTransport();
            logger.Trace("Сервис остановлен.");
        }


        protected virtual void InitUnhandledExceptionHandler()
        {
            logger.Trace("Init Unhandled Exception Handler");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                logger.Fatal((Exception) args.ExceptionObject, $"AppDomain.UnhandledException:\r\n");

                try
                {
                    // var eventBus = Container.Resolve<IEventBus>();
                    //  eventBus.RaiseExceptionDetectEvent((Exception)args.ExceptionObject);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, $"TaskScheduler.UnobservedTaskException: При RaiseExceptionDetectEvent произошла ошибка ");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.Exception.Handle(exp =>
                {
                    if (exp is OperationCanceledException)
                        return true;

                    logger.Fatal(exp, $"TaskScheduler.UnobservedTaskException:\r\n");


                    try
                    {
                        //    var eventBus = Container.Resolve<IEventBus>();
                        //     exp.RaiseExceptionDetectEvent(eventBus);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex, $"TaskScheduler.UnobservedTaskException: При RaiseExceptionDetectEvent произошла ошибка ");
                    }

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
                .AppendLine($"RunnerType      : {GetType().Name}")
                .AppendLine($"Contour         : {AdapterConfiguration.Contour}")
                .AppendLine($"AdapterType     : {AdapterConfiguration.AdapterType}")
                .AppendLine($"AdapterName     : {AdapterConfiguration.AdapterName}")
                .AppendLine($"AdapterVersion  : {AdapterConfiguration.AdapterVersion}")
                .AppendLine($"AdapterHostName : {AdapterConfiguration.AdapterHostName}")
                .AppendLine($"AdapterHostIp   : {string.Join(", ", AdapterConfiguration.AdapterHostIp)}")
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