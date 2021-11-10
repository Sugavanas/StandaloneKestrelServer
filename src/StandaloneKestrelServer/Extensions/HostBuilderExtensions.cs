using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StandaloneKestrelServer.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseStandaloneKestrelServer(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, service) =>
            {
                service.AddOptions<StandaloneKestrelServerOptions>();
                service.AddHostedService<StandaloneKestrelServerService>();
                service.Configure<StandaloneKestrelServerOptions>(
                    context.Configuration.GetSection("StandaloneKestrel"));
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
            return hostBuilder.ConfigureServices
            (
                (context, service) => { service.Configure(options); }
            );
        }
    }
}