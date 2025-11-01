using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS.StandaloneKestrelServer.Extensions;

namespace SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

            /*
              // Sample for Minimal API

              var builder = WebApplication.CreateBuilder(args);

              builder.UseStandaloneKestrelServer(options =>
              {
                  options.KestrelServerOptions.ListenLocalhost(8050);
                  options.ConfigureRequestPipeline(ConfigureRequestPipeline);
              });

              builder.Services.AddRoutingCore();

              var app = builder.Build();

              app.MapGet("/", (HttpContext httpContext) => "Default Kestrel Server");

              app.Run();
          */
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddRouting())
                .UseStandaloneKestrelServer(options =>
                {
                    options.KestrelServerOptions.ListenLocalhost(8050);
                    options.ConfigureRequestPipeline(ConfigureRequestPipeline);
                });

        private static void ConfigureRequestPipeline(IApplicationBuilder builder)
        {
            builder.UseRouting();

            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/",
                    async context =>
                    {
                        await context.Response.WriteAsync("Hello World! - Standalone Kestrel Server");
                    });
            });

            builder.Use(next => async context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await next(context);
            });
        }
    }
}