using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAL.Core.NLogEx;

namespace SAL.Core.Service
{
    public class BackAdapterRunner 
    {
        public async Task RunAsync()
        {
            

            var adapter = new BackAdapter();
            adapter.LoadConfiguration();

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
                .UseNlog(adapter.LogFactory)
                .Build();

            adapter.Initialization();

            await host.RunAsync();
        }
    }
}



