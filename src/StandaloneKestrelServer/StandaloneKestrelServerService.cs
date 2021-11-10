using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StandaloneKestrelServer
{
    public class StandaloneKestrelServerService : IHostedService
    {
        protected StandaloneKestrelServerOptions ServerOptions { get; set; }

        protected StandaloneKestrelServer Server { get; set; }

        private ILogger _logger;

        private ILoggerFactory _loggerFactory;

        private IServiceProvider _serviceProvider;

        private IHostApplicationLifetime _applicationLifetime;

        public StandaloneKestrelServerService(IOptions<StandaloneKestrelServerOptions> serverOptions,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime)
        {
            ServerOptions = serverOptions.Value ?? new StandaloneKestrelServerOptions();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<StandaloneKestrelServerService>();
            _applicationLifetime = applicationLifetime;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Server {Name}", ServerOptions.Name);

            Server ??= CreateServer();
            if (Server == null)
            {
                _applicationLifetime.StopApplication();
                return;
            }

            var addresses = GetAddressesOrDefault();

            var applicationBuilder = new ApplicationBuilder(_serviceProvider);

            ServerOptions.ConfigureRequestPipeline(GetLastMiddleware());
            ServerOptions.RequestPipeline.Invoke(applicationBuilder);

            var requestPipeline = applicationBuilder.Build();

            var app = new Application(requestPipeline, _loggerFactory);

            await Server.StartAsync(app, cancellationToken);

            if (addresses != null)
            {
                foreach (var address in addresses)
                {
                    _logger.LogInformation("{Name}: Now listening on: {Address}", ServerOptions.Name, address);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Server.StopAsync(cancellationToken);
        }

        protected virtual StandaloneKestrelServer CreateServer()
        {
            try
            {
                var server =
                    (StandaloneKestrelServer) ActivatorUtilities.CreateInstance(_serviceProvider,
                        ServerOptions.RealServerType);
                _logger.LogDebug("Using Server: {ServerType}",  ServerOptions.RealServerType.AssemblyQualifiedName);
                return server;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Could not create server instance");
                return null;
            }
        }

        protected virtual ICollection<string> GetAddressesOrDefault()
        {
            var serverAddressesFeature = Server.Features?.Get<IServerAddressesFeature>();
            var addresses = serverAddressesFeature?.Addresses;
            if (addresses is {IsReadOnly: false, Count: 0})
            {
                _logger.LogInformation("Listen Endpoint not configured for {ServerName}. Listening to default endpoint",
                    ServerOptions.Name);
                addresses.Add("http://localhost:8080");
            }

            return addresses;
        }

        protected virtual Action<IApplicationBuilder> GetLastMiddleware() =>
            builder => builder.Use(_ => _ => Task.CompletedTask);
    }
}