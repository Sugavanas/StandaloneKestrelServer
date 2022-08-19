using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServerOptions
    {
        public string Name { get; set; } = "StandaloneKestrelServer";

        public KestrelServerOptions KestrelServerOptions { get; set; } = new();

        public StandaloneKestrelServerConfigurationLoader? ConfigurationLoader { get; protected set; }

        public string ServerType
        {
            get => _serverType.AssemblyQualifiedName ??
                   throw new Exception("Unexpected error while trying to get ServerType");
            set => UseServer(value);
        }

        public Type RealServerType => _serverType;

        public Action<IApplicationBuilder> RequestPipeline { get; set; } = default;

        private Type _serverType = typeof(StandaloneKestrelServer);

        public StandaloneKestrelServerOptions ConfigureKestrel(Action<KestrelServerOptions> options)
        {
            options(KestrelServerOptions);
            return this;
        }

        public StandaloneKestrelServerOptions ConfigureRequestPipeline(Action<IApplicationBuilder> builder)
        {
            RequestPipeline += builder;
            return this;
        }

        public StandaloneKestrelServerOptions UseServer(string server)
        {
            var type = Type.GetType(server);

            if (type == null)
                throw new Exception($"{server} was not found.");

            return UseServer(type);
        }

        public StandaloneKestrelServerOptions UseServer(Type serverType)
        {
            if (serverType != typeof(StandaloneKestrelServer) &&
                !serverType.IsSubclassOf(typeof(StandaloneKestrelServer)))
            {
                throw new Exception(
                    $"{serverType.FullName} needs to extend from {typeof(StandaloneKestrelServer).FullName}");
            }

            _serverType = serverType;
            return this;
        }

        public StandaloneKestrelServerOptions Configure(IConfiguration configuration, bool reloadOnChange)
        {
            ConfigurationLoader = new StandaloneKestrelServerConfigurationLoader(configuration, reloadOnChange);
            ConfigureKestrel(configuration.GetSection("Kestrel"), reloadOnChange);
            return this;
        }

        internal StandaloneKestrelServerOptions ConfigureKestrel(IConfiguration config, bool reloadOnChange)
        {
            KestrelServerOptions.Configure(config, reloadOnChange);
            return this;
        }
    }
}