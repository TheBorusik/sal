using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace SAL.Core.NLogEx
{
    public static class AuthServerNlogExtensions
    {
        public static IHostBuilder UseNlog(this IHostBuilder builder, LogFactory logFactory)
        {
            return builder.UseNlog(logFactory, null);
        }
        public static IHostBuilder UseNlog(
            this IHostBuilder builder,
            LogFactory logFactory,
            NLogProviderOptions options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            builder.ConfigureServices((h, s) =>
                ConfigureServicesNLog(logFactory, options, s));
            return builder;
        }


        private static void ConfigureServicesNLog(
            LogFactory logFactory,
            NLogProviderOptions options,
            IServiceCollection services)
        {
            services.AddSingleton(serviceProvider =>
            {
                var nlogLoggerProvider = new NLogLoggerProvider(options ?? new NLogProviderOptions(), logFactory);
                return (ILoggerProvider)nlogLoggerProvider;
            });
        }


    }
}
