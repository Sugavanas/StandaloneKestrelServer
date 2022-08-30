using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TS.StandaloneKestrelServer.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseStandaloneKestrelServer(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, service) =>
            {
                service.AddStandaloneKestrelServerServices();

                service.Configure<StandaloneKestrelServerOptions>(
                    context.Configuration.GetSection("StandaloneKestrel"));
                service.Configure((StandaloneKestrelServerOptions options) =>
                    options.Configure(
                        context.Configuration.GetSection("StandaloneKestrel"),
                        true));
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
                (_, service) =>
                {
                    service.Configure(options);
                }
            );
        }
    }
}