using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StandaloneKestrelServer;
using StandaloneKestrelServer.Extensions;

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
                .UseStandaloneKestrelServer(options =>
                {
                    options.ConfigureRequestPipeline(builder =>
                    {
                        builder.Use(next =>
                            async context =>
                            {
                                await context.Response.WriteAsync("It Works!");
                                await next(context);
                            });
                    });
                });
    }
}