using Microsoft.AspNetCore.Http;
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
                .UseStandaloneKestrelServer(options =>
                {
                    options.KestrelServerOptions.ListenLocalhost(8050);
                    options.ConfigureRequestPipeline(builder =>
                    {
                        builder.Use(next =>
                            async context =>
                            {
                                SampleObject x = context.GetPersistentContainer()?.Get<SampleObject>();

                                if (x is null)
                                {
                                    x = new SampleObject()
                                    {
                                        Count = 0
                                    };
                                    context.GetPersistentContainer()?.Set(x);
                                }

                                x.Count++;
                                await next(context);
                            });

                        builder.Use(next =>
                            async context =>
                            {
                                var x = context.GetPersistentContainer()?.Get<SampleObject>();
                                await context.Response.WriteAsync("It Works! Count: " + (x?.Count ?? -1));
                                await next(context);
                            });
                    });
                });
    }

    public class SampleObject
    {
        public int Count { get; set; }
    }
}