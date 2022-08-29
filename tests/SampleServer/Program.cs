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
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });

            builder.Use(next => async context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await next(context);
            });
        }
    }
}