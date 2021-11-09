using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StandaloneKestrelServer;

namespace SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configureLogging => configureLogging.SetMinimumLevel(LogLevel.Debug))
                .ConfigureServices(services =>
                {
                    services.AddHostedService<StandaloneKestrelServerService>();
                    services.Configure<StandaloneKestrelServerOptions>(options =>
                    {
                        options.RequestPipeline = builder =>
                        {
                            builder.Use(next =>
                                async context => await context.Response.WriteAsync("It Works!"));
                        };
                    });
                });
    }
}