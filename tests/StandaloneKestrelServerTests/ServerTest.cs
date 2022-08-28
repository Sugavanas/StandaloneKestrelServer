using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using TS.StandaloneKestrelServer;
using TS.StandaloneKestrelServer.Extensions;
using Xunit;

namespace StandaloneKestrelServerTests
{
    public class ServerTest
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
                options.UseServer(typeof(HttpTestServer).AssemblyQualifiedName!));
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
        public async Task CheckServerTypeChangeOnConfigurationReload()
        {
            var serverTypeMock = new Mock<IConfigurationSection>();
            var applicationTypeMock = new Mock<IConfigurationSection>();
            var configurationMock = new Mock<IConfigurationSection>();
            var reloadTokenMock = new Mock<IChangeToken>();

            object? callbackObject = null;
            Action<object>? callback = null;
            int called = 0;

            serverTypeMock.SetupGet(c => c.Value).Returns(typeof(HttpTestServer).AssemblyQualifiedName ?? "");
            applicationTypeMock.SetupGet(c => c.Value).Returns(typeof(Application).AssemblyQualifiedName ?? "");
            reloadTokenMock.Setup(c => c.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
                .Callback<Action<object>, object>((action, o) =>
                {
                    callback = action;
                    callbackObject = o;
                    called++;
                });

            configurationMock.Setup(c => c.GetSection("ServerType")).Returns(serverTypeMock.Object);
            configurationMock.Setup(c => c.GetSection("ApplicationType")).Returns(applicationTypeMock.Object);
            configurationMock.Setup(c => c.GetReloadToken()).Returns(reloadTokenMock.Object);

            using var service = new HttpTestServerService(options =>
            {
                options.UseServer(typeof(HttpTestServer).AssemblyQualifiedName!);
                options.Configure(configurationMock.Object, true);
            });
            var server = GetServer(service.HostedService);

            Assert.NotNull(server);
            Assert.IsType<HttpTestServer>(server);
            Assert.IsAssignableFrom<StandaloneKestrelServer>(server);

            Assert.NotNull(callbackObject);
            Assert.NotNull(callback);

            serverTypeMock.SetupGet(c => c.Value).Returns(typeof(StandaloneKestrelServer).AssemblyQualifiedName ?? "");

            callback(callbackObject);

            // The configuration is reloaded asynchronously 
            var task = Task.Run(async () =>
            {
                while (called != 2)
                {
                    await Task.Delay(500);
                }
            });
            await task.WaitAsync(TimeSpan.FromMinutes(1));

            server = GetServer(service.HostedService);

            Assert.Equal(2, called);
            Assert.NotNull(server);
            Assert.IsType<StandaloneKestrelServer>(server);

            serverTypeMock.SetupGet(c => c.Value).Returns(typeof(HttpTestServer).AssemblyQualifiedName ?? "");

            callback(callbackObject);

            // The configuration is reloaded asynchronously  
            task = Task.Run(async () =>
            {
                while (called != 2)
                {
                    await Task.Delay(500);
                }
            });
            await task.WaitAsync(TimeSpan.FromMinutes(1));
            server = GetServer(service.HostedService);

            Assert.NotNull(server);
            Assert.IsType<HttpTestServer>(server);
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

        [Fact]
        public void CheckCustomApplicationTypeWhenUsingType()
        {
            using var service = new HttpTestServerService(options =>
                options.UseApplication(typeof(TestApplication)));
            var application = GetApplication(service.HostedService);
            Assert.NotNull(application);
            Assert.IsType<TestApplication>(application);
            Assert.IsAssignableFrom<Application>(application);
        }

        private StandaloneKestrelServer? GetServer(StandaloneKestrelServerService hostedService)
        {
            var property =
                typeof(StandaloneKestrelServerService).GetProperty("Server",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            var server = property?.GetValue(hostedService);
            return (StandaloneKestrelServer?) server; //if it's null, doesn't matter.
        }

        private Application? GetApplication(StandaloneKestrelServerService hostedService)
        {
            var field =
                typeof(StandaloneKestrelServerService).GetField("_application",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            var application = field?.GetValue(hostedService);
            return (Application?) application;
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

            serviceCollection
                .AddTransient<IConfigureOptions<StandaloneKestrelServerOptions>, StandaloneKestrelServerOptionsSetup>();
            serviceCollection.AddOptions<StandaloneKestrelServerOptions>();
            serviceCollection.Configure<StandaloneKestrelServerOptions>(configureOptions);

            serviceCollection.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            serviceCollection.AddSingleton<IHostApplicationLifetime, ApplicationLifetime>();
            serviceCollection.AddSingleton<IHostEnvironment, HostingEnvironment>();

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

    internal class TestApplication : Application
    {
        public TestApplication(RequestDelegate requestPipeline, ILoggerFactory loggerFactory,
            IHttpContextFactory httpContextFactory, IServiceProvider serviceProvider) : base(requestPipeline,
            loggerFactory, httpContextFactory)
        {
        }
    }
}