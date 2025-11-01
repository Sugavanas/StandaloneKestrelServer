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
using Microsoft.Extensions.Primitives;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServerService : IHostedService
    {
        protected StandaloneKestrelServerOptions ServerOptions { get; set; }

        protected StandaloneKestrelServer? Server { get; set; }

        private readonly ILogger _logger;

        private readonly ILoggerFactory _loggerFactory;

        private readonly IServiceProvider _serviceProvider;

        private Application? _application = null;

        private RequestDelegate? _requestPipeline = null;

        private bool _stopping = false;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public StandaloneKestrelServerService(IOptions<StandaloneKestrelServerOptions> serverOptions,
            ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            ServerOptions = serverOptions.Value ?? new StandaloneKestrelServerOptions();
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<StandaloneKestrelServerService>();
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
                    RegisterConfigurationChangeCallback(reloadToken);
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

        protected virtual void CreateServer()
        {
            if (Server is not null)
                return;

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

        protected virtual RequestDelegate GetRequestPipeline()
        {
            if (_requestPipeline is not null)
                return _requestPipeline;

            var applicationBuilder = new ApplicationBuilder(_serviceProvider);

            ServerOptions.ConfigureRequestPipeline(GetLastMiddleware());
            ServerOptions.RequestPipeline?.Invoke(applicationBuilder);

            _requestPipeline = applicationBuilder.Build();
            return _requestPipeline;
        }

        protected virtual Application GetApplication()
        {
            if (_application is not null)
                return _application;

            var requestPipeline = GetRequestPipeline();
            var httpContextFactory = _serviceProvider.GetService<IHttpContextFactory>() ??
                                     new DefaultHttpContextFactory(_serviceProvider);

            var applicationType = ServerOptions.RealApplicationType;

            _logger.LogDebug("Using Application Type: {ApplicationType}", applicationType.AssemblyQualifiedName);

            if (applicationType == typeof(Application))
            {
                _application = new Application(requestPipeline, _loggerFactory, httpContextFactory);
                return _application;
            }
            
            var ctorArgs = new object[] {requestPipeline, httpContextFactory};
            var application = ActivatorUtilities.CreateInstance(_serviceProvider, applicationType, ctorArgs);

            if (application is null or not Application)
            {
                throw new Exception("Unexpected error while creating application.");
            }

            _application = (Application) application;
            return _application;
        }

        protected virtual Action<IApplicationBuilder> GetLastMiddleware() =>
            builder => builder.Use(_ => _ => Task.CompletedTask);

        #region Configuration Relaoder

        protected virtual void RegisterConfigurationChangeCallback(IChangeToken? reloadToken)
        {
            static void ConfigurationChangeCallback(object? state)
            {
                if (state is null)
                    return;
                
                var service = (StandaloneKestrelServerService) state;
                Task.Run(service.OnConfigurationChangeAsync);
            }

            reloadToken?.RegisterChangeCallback(ConfigurationChangeCallback, this);
        }

        protected virtual async Task OnConfigurationChangeAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                if (_stopping)
                    return;

                _logger.LogDebug("Configuration changed. Checking for changes");

                var newServerType = ServerOptions.ConfigurationLoader?.GetServerType();
                CheckConfigurationServerType(newServerType, out var reloadServer);

                var newApplicationType = ServerOptions.ConfigurationLoader?.GetApplicationType();
                CheckConfigurationApplicationType(newApplicationType, out var reloadApplication);

                if (!reloadServer && !reloadApplication)
                {
                    return;
                }

                _logger.LogInformation(
                    "Restarting Standalone Kestrel Server Service to use {NewServerType} with Application {NewApplicationType}",
                    ServerOptions.RealServerType, ServerOptions.RealApplicationType);
                var oldServer = Server;
                Server = null;

                CreateServer();

                if (oldServer is not null)
                {
                    await oldServer.StopAsync(CancellationToken.None);
                    if (oldServer is IDisposable disposable)
                        oldServer.Dispose();
                }

                await StartServer(CancellationToken.None);
            }
            finally
            {
                var reloadToken = ServerOptions.ConfigurationLoader?.GetReloadToken();
                RegisterConfigurationChangeCallback(reloadToken);
                _semaphoreSlim.Release();
            }
        }

        protected void CheckConfigurationServerType(string? newServerType, out bool mustReload)
        {
            mustReload = false;
            if (newServerType is null)
            {
                return;
            }

            var actualServerType = Type.GetType(newServerType);
            if (actualServerType is null)
            {
                _logger.LogError("Could not reload Kestrel Server. Invalid type given: {Type}",
                    newServerType);
                return;
            }

            if (actualServerType == Server?.GetType())
            {
                return;
            }

            ServerOptions.ServerType = newServerType;
            mustReload = true;
        }

        protected void CheckConfigurationApplicationType(string? newApplicationType, out bool mustReload)
        {
            mustReload = false;
            if (newApplicationType is null)
            {
                return;
            }

            var actualApplicationType = Type.GetType(newApplicationType);

            if (actualApplicationType is null)
            {
                _logger.LogError("Could not reload Kestrel Application. Invalid type given: {Type}",
                    newApplicationType);
                return;
            }

            if (actualApplicationType == GetApplication().GetType())
            {
                return;
            }

            ServerOptions.ApplicationType = newApplicationType;

            //Set application to null so that it will be recreated.
            // ReSharper disable once SuspiciousTypeConversion.Global - Derived classes are.
            if (_application is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _application = null;
            mustReload = true;
        }

        #endregion
    }
}