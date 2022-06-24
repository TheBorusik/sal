using System;
using System.Threading.Tasks;
using Autofac;
using Prometheus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAL.Core.Exceptions;
using Microsoft.AspNetCore.Hosting;

namespace SAL.Core.Service
{
    public class FrontAdapterRunner 
    {
        public async Task RunAsync()
        {
            var adapter = new FrontAdapter();
            try
            {
                adapter.LoadConfiguration();
            }
            catch (ConfigurationErrorException e)
            {
                Console.WriteLine($"InitConfiguration error: {e.Message}");
                return;
            }

            var host = new HostBuilder()
                .UseServiceProviderFactory(adapter)
                .ConfigureServices((hc, services) =>
                {
                    services.AddHostedService<LifetimeEventsHostedService>();
                })
                .ConfigureContainer<ContainerBuilder>((hostContent, builder) =>
                {
                    builder.RegisterInstance<ISalService>(adapter);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureWebHostDefaults(webBuilder => 
                {
                    webBuilder.Configure(app => { app.UseMetricServer(); });
                })
                .Build();

            try
            {
                adapter.Initialization();
            }
            catch (ConfigurationErrorException e)
            {
                Console.WriteLine($"Adapter Initialization error: {e.Message}");
                return;
            }
            try
            {
                adapter.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Starting error: {e.Message}");
                return;
            }
            
            
            try
            {
                await host.RunAsync();
            }
            catch (OperationCanceledException)
            {
                //
            }
            adapter.Done();

        }
    }
}



