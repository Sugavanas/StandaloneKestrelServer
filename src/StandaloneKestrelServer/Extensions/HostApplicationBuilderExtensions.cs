using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TS.StandaloneKestrelServer.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        public static IHostApplicationBuilder UseStandaloneKestrelServer(this IHostApplicationBuilder hostBuilder)
        {
            var section = hostBuilder.Configuration.GetSection(Constants.ConfigurationSectionKey);
            return hostBuilder.UseStandaloneKestrelServer(section);
        }

        public static IHostApplicationBuilder UseStandaloneKestrelServer(this IHostApplicationBuilder hostBuilder,
            IConfigurationSection section)
        {
            hostBuilder.Services.AddStandaloneKestrelServerServices();
            hostBuilder.Services.ConfigureStandaloneKestrelServer(section);
            return hostBuilder;
        }

        public static IHostApplicationBuilder UseStandaloneKestrelServer(this IHostApplicationBuilder hostBuilder,
            Action<StandaloneKestrelServerOptions> configureOptions)
        {
            hostBuilder.UseStandaloneKestrelServer();
            hostBuilder.Services.ConfigureStandaloneKestrelServer(configureOptions);
            return hostBuilder;
        }
        
    }
}