using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TS.StandaloneKestrelServer;
using TS.StandaloneKestrelServer.Extensions;
using Xunit;

namespace StandaloneKestrelServerTests
{
    public class ServerOptionsTest
    {
        [Fact]
        public void KestrelServerOptionsNotNull()
        {
            var options = new StandaloneKestrelServerOptions();
            Assert.NotNull(options.KestrelServerOptions);
        }

        [Fact]
        public void KestrelServerOptionsApplicationServicesNotNull()
        {
            var hostBuilder = new HostBuilder().UseStandaloneKestrelServer();

            hostBuilder.ConfigureServices(services =>
            {
                var provider = services.BuildServiceProvider();
                Assert.NotNull(provider);

                var setup = provider.GetRequiredService<IConfigureOptions<StandaloneKestrelServerOptions>>();
                var options = provider.GetRequiredService<IOptions<StandaloneKestrelServerOptions>>();
                Assert.NotNull(setup);
                Assert.NotNull(options);

                setup.Configure(options.Value);
                Assert.NotNull(options.Value.KestrelServerOptions.ApplicationServices);
            });
            hostBuilder.Build();
        }


        [Fact]
        public void ConfigureKestrelCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            httpServerOptions.ConfigureKestrel(options =>
            {
                Assert.NotNull(options);
                Assert.IsType<KestrelServerOptions>(options);
            });
        }

        [Fact]
        public void UseServerTypeCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            httpServerOptions.UseServer(typeof(HttpTestServer).AssemblyQualifiedName!);
            Assert.True(httpServerOptions.RealServerType == typeof(HttpTestServer));
            Assert.True(httpServerOptions.ServerType == typeof(HttpTestServer).AssemblyQualifiedName);
        }

        [Fact]
        public void UseServerTypeDefaultCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            Assert.True(httpServerOptions.RealServerType == typeof(StandaloneKestrelServer));
            Assert.True(httpServerOptions.ServerType == typeof(StandaloneKestrelServer).AssemblyQualifiedName);
        }

        [Fact]
        public void UseServerTypeInvalidCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            Assert.Throws<Exception>(() =>
                httpServerOptions.UseServer(typeof(InvalidHttpTestServer).AssemblyQualifiedName!));
        }

        [Fact]
        public void UseApplicationTypeCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            httpServerOptions.UseApplication(typeof(TestApplication).AssemblyQualifiedName!);
            Assert.True(httpServerOptions.RealApplicationType == typeof(TestApplication));
            Assert.True(httpServerOptions.ApplicationType == typeof(TestApplication).AssemblyQualifiedName);
        }

        [Fact]
        public void UseApplicationTypeDefaultCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            Assert.True(httpServerOptions.RealApplicationType == typeof(Application));
            Assert.True(httpServerOptions.ApplicationType == typeof(Application).AssemblyQualifiedName);
        }

        [Fact]
        public void UseApplicationTypeInvalidCheck()
        {
            var httpServerOptions = new StandaloneKestrelServerOptions();
            Assert.Throws<Exception>(() =>
                httpServerOptions.UseApplication(typeof(InvalidApplication).AssemblyQualifiedName!));
        }

        internal class InvalidHttpTestServer : IServer
        {
            public IFeatureCollection Features { get; } = new FeatureCollection();

            public Task StartAsync<TContext>(IHttpApplication<TContext> application,
                CancellationToken cancellationToken) where TContext : notnull
            {
                throw new System.NotImplementedException();
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public void Dispose()
            {
                throw new System.NotImplementedException();
            }
        }

        internal class InvalidApplication : IHttpApplication<InvalidApplication.Context>
        {
            public Context CreateContext(IFeatureCollection contextFeatures)
            {
                throw new NotImplementedException();
            }

            public Task ProcessRequestAsync(Context context)
            {
                throw new NotImplementedException();
            }

            public void DisposeContext(Context context, Exception? exception)
            {
                throw new NotImplementedException();
            }

            public class Context
            {
            }
        }
    }
}