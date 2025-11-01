using System;
using Microsoft.Extensions.Hosting;

namespace TS.StandaloneKestrelServer.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseStandaloneKestrelServer(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, service) =>
            {
                service.AddStandaloneKestrelServerServices();
                service.ConfigureStandaloneKestrelServer(context.Configuration.GetSection(Constants.ConfigurationSectionKey));
            });
        }

        public static IHostBuilder UseStandaloneKestrelServer(this IHostBuilder hostBuilder,
            Action<StandaloneKestrelServerOptions> options)
        {
            return hostBuilder.UseStandaloneKestrelServer().ConfigureStandaloneKestrelServer(options);
        }
        
        public static IHostBuilder ConfigureStandaloneKestrelServer(this IHostBuilder hostBuilder,
            Action<StandaloneKestrelServerOptions> options)
        {
            return hostBuilder.ConfigureServices((_, service) =>
            {
                service.ConfigureStandaloneKestrelServer(options);
            });
        }
    }
}