using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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


        public StandaloneKestrelServerService(IOptions<StandaloneKestrelServerOptions> serverOptions,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime)
        {
            ServerOptions = serverOptions.Value ?? new StandaloneKestrelServerOptions();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<StandaloneKestrelServerService>();

            ServerOptions.KestrelServerOptions.ApplicationServices = serviceProvider;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Server {Name}", ServerOptions.Name);

            Server ??= CreateServer();
            if (Server == null)
            {
                //TODO: Exit
            }

            ConfigureDefaultAddress();

            var app = new Application(_loggerFactory);
            await Server.StartAsync(app, cancellationToken);
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
                        ServerOptions.ServerType);
                return server;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Could not create server instance");
                return null;
            }
        }

        protected virtual void ConfigureDefaultAddress()
        {
            var serverAddressesFeature = Server.Features?.Get<IServerAddressesFeature>();
            var addresses = serverAddressesFeature?.Addresses;
            if (addresses is {IsReadOnly: false, Count: 0})
            {
                _logger.LogInformation("Listen Endpoint not configured for {ServerName}. Listening to default endpoint",
                    ServerOptions.Name);
                addresses.Add("http://localhost:8080");
            }
        }
    }
}