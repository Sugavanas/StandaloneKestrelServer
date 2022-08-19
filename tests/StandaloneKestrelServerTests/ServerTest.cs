using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TS.StandaloneKestrelServer;
using TS.StandaloneKestrelServer.Extensions;
using Xunit;

namespace StandaloneKestrelServerTests
{
    public class ServerTest : IDisposable
    {
        [Fact]
        public void CheckExtension()
        {
            var hostBuilder = Host.CreateDefaultBuilder().UseStandaloneKestrelServer();

            ServiceProvider? serviceProvider = null;
            hostBuilder.ConfigureServices(services => { serviceProvider = services.BuildServiceProvider(); });
            hostBuilder.Build();

            Assert.NotNull(serviceProvider);

            var hostedServices = serviceProvider?.GetServices<IHostedService>();
            Assert.NotNull(hostedServices);
            Assert.True(hostedServices?.Any(service => service.GetType() == typeof(StandaloneKestrelServerService)));
            
            var options = serviceProvider?.GetRequiredService<IOptions<StandaloneKestrelServerOptions>>();
            Assert.NotNull(options);
        }

        [Fact]
        public void CheckDefaultServerIsNotNull()
        {
            using var service = new HttpTestServerService();
            var server = GetServer(service.HostedService);
            Assert.NotNull(server);
            Assert.IsType<StandaloneKestrelServer>(server);
        }


        [Fact]
        public void CheckCustomServerTypeWhenUsingString()
        {
            using var service = new HttpTestServerService(options =>
                options.UseServer(typeof(HttpTestServer).AssemblyQualifiedName));
            var server = GetServer(service.HostedService);
            Assert.NotNull(server);
            Assert.IsType<HttpTestServer>(server);
            Assert.IsAssignableFrom<StandaloneKestrelServer>(server);
        }

        [Fact]
        public void CheckCustomServerTypeWhenUsingType()
        {
            using var service = new HttpTestServerService(options =>
                options.UseServer(typeof(HttpTestServer)));
            var server = GetServer(service.HostedService);
            Assert.NotNull(server);
            Assert.IsType<HttpTestServer>(server);
            Assert.IsAssignableFrom<StandaloneKestrelServer>(server);
        }

        [Fact]
        public void CheckListeningPorts()
        {
            using var service = new HttpTestServerService(options =>
                options.KestrelServerOptions.ListenLocalhost(1234));
            var server = GetServer(service.HostedService);
            var addresses = server?.Features?.Get<IServerAddressesFeature>()?.Addresses;
            Assert.True(addresses?.Contains("http://localhost:1234"));
        }

        private StandaloneKestrelServer? GetServer(StandaloneKestrelServerService hostedService)
        {
            var property =
                typeof(StandaloneKestrelServerService).GetProperty("Server",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            var server = property?.GetValue(hostedService);
            return (StandaloneKestrelServer?) server; //if it's null, doesn't matter.
        }

        public void Dispose()
        {
        }
    }

    internal class HttpTestServerService : IDisposable
    {
        public StandaloneKestrelServerService HostedService { get; private set; }

        public HttpTestServerService() : this(_ => { })
        {
        }

        public HttpTestServerService(Action<StandaloneKestrelServerOptions> configureOptions)
        {
            var serviceCollection = new ServiceCollection();
            

            serviceCollection.AddTransient<IConfigureOptions<StandaloneKestrelServerOptions>, StandaloneKestrelServerOptionsSetup>();
            serviceCollection.AddOptions<StandaloneKestrelServerOptions>();
            serviceCollection.Configure<StandaloneKestrelServerOptions>(configureOptions);
            
            serviceCollection.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            serviceCollection.AddSingleton<IHostApplicationLifetime, ApplicationLifetime>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            HostedService = ActivatorUtilities.CreateInstance<StandaloneKestrelServerService>(serviceProvider);
            HostedService.StartAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {
            HostedService.StopAsync(CancellationToken.None).Wait();
        }
    }

    internal class HttpTestServer : StandaloneKestrelServer
    {
        public HttpTestServer(IOptions<StandaloneKestrelServerOptions> standaloneKestrelServerOptions,
            ILoggerFactory loggerFactory) : base(standaloneKestrelServerOptions, loggerFactory)
        {
        }

        public HttpTestServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory,
            ILoggerFactory loggerFactory) : base(options, transportFactory, loggerFactory)
        {
        }
    }
}