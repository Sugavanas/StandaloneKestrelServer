using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServerService : IHostedService
    {
        protected StandaloneKestrelServerOptions ServerOptions { get; set; }

        protected StandaloneKestrelServer? Server { get; set; }

        private readonly ILogger _logger;

        private readonly ILoggerFactory _loggerFactory;

        private readonly IServiceProvider _serviceProvider;

        private IHostApplicationLifetime _applicationLifetime;

        private Application? _application = null;

        private bool _stopping = false;

        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

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
            await _semaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                CreateServer();
                await StartServer(cancellationToken);

                if (ServerOptions.ConfigurationLoader?.ReloadOnChange == true)
                {
                    var reloadToken = ServerOptions.ConfigurationLoader?.GetReloadToken();
                    reloadToken?.RegisterChangeCallback(
                        state => ((StandaloneKestrelServerService) state).OnConfigurationChange().GetAwaiter()
                            .GetResult(),
                        this
                    );
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                _stopping = true;
                if (Server is not null)
                    await Server.StopAsync(cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        protected virtual async Task OnConfigurationChange()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                if (_stopping)
                    return;

                _logger.LogDebug("Configuration changed. Checking for changes");

                var newServerType = ServerOptions.ConfigurationLoader?.GetServerType();

                if (newServerType is null)
                    return;

                var actualServerType = Type.GetType(newServerType);

                if (actualServerType is null || actualServerType == Server?.GetType())
                    return;

                _logger.LogInformation("Restarting Standalone Kestrel Server Service to use {NewServerType}",
                    newServerType);
                ServerOptions.ServerType = newServerType;

                var oldServer = Server;
                CreateServer();

                if (oldServer is not null)
                    await oldServer.StopAsync(CancellationToken.None);

                await StartServer(CancellationToken.None);
            }
            finally
            {
                var reloadToken = ServerOptions.ConfigurationLoader?.GetReloadToken();
                reloadToken?.RegisterChangeCallback(
                    async state => await ((StandaloneKestrelServerService) state).OnConfigurationChange(),
                    this
                );
                _semaphoreSlim.Release();
            }
        }

        protected virtual void CreateServer()
        {
            try
            {
                var server =
                    (StandaloneKestrelServer) ActivatorUtilities.CreateInstance(_serviceProvider,
                        ServerOptions.RealServerType);
                _logger.LogDebug("Using Server: {ServerType}", ServerOptions.RealServerType.AssemblyQualifiedName);
                Server = server;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Could not create server instance");
                Server = null;
            }
        }

        protected virtual async Task StartServer(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Server {Name}", ServerOptions.Name);

            if (Server is null)
            {
                _logger.LogCritical("Server is not found. Cannot start server");
                return;
            }

            var app = GetApplication();
            await Server.StartAsync(app, cancellationToken);

            var addresses = GetAddressesOrDefault();
            if (addresses is not null)
            {
                foreach (var address in addresses)
                {
                    _logger.LogInformation("{Name}: Now listening on: {Address}", ServerOptions.Name, address);
                }
            }
        }

        protected ICollection<string>? GetAddressesOrDefault()
        {
            var serverAddressesFeature = Server?.Features?.Get<IServerAddressesFeature>();
            var addresses = serverAddressesFeature?.Addresses;
            if (addresses is {IsReadOnly: false, Count: 0})
            {
                _logger.LogInformation(
                    "Listen Endpoint not configured for {ServerName}. Listening to default endpoints",
                    ServerOptions.Name);
            }

            return addresses;
        }

        protected virtual Application GetApplication()
        {
            if (_application != null)
                return _application;

            var applicationBuilder = new ApplicationBuilder(_serviceProvider);

            ServerOptions.ConfigureRequestPipeline(GetLastMiddleware());
            ServerOptions.RequestPipeline?.Invoke(applicationBuilder);

            var requestPipeline = applicationBuilder.Build();
            var httpContextFactory = _serviceProvider.GetService<IHttpContextFactory>() ??
                                     new DefaultHttpContextFactory(_serviceProvider);

            _application = new Application(requestPipeline, _loggerFactory, httpContextFactory);
            return _application;
        }

        protected virtual Action<IApplicationBuilder> GetLastMiddleware() =>
            builder => builder.Use(_ => _ => Task.CompletedTask);
    }
}