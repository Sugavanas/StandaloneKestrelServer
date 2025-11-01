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
            get => RealServerType.AssemblyQualifiedName ??
                   throw new Exception("Unexpected error while trying to get " + nameof(ServerType));
            set => UseServer(value);
        }

        public Type RealServerType { get; protected set; } = typeof(StandaloneKestrelServer);

        public string ApplicationType
        {
            get => RealApplicationType.AssemblyQualifiedName ??
                   throw new Exception("Unexpected error while trying to get " + nameof(ApplicationType));
            set => UseApplication(value);
        }

        public Type RealApplicationType { get; protected set; } = typeof(Application);

        public Action<IApplicationBuilder>? RequestPipeline { get; set; }

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
                throw new Exception($"{server} type was not found.");

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

            RealServerType = serverType;
            return this;
        }

        public StandaloneKestrelServerOptions UseApplication(string application)
        {
            var type = Type.GetType(application);

            if (type == null)
                throw new Exception($"{application} type was not found.");

            return UseApplication(type);
        }

        public StandaloneKestrelServerOptions UseApplication(Type applicationType)
        {
            if (applicationType != typeof(Application) &&
                !applicationType.IsSubclassOf(typeof(Application)))
            {
                throw new Exception(
                    $"{applicationType.FullName} needs to extend from {typeof(Application).FullName}");
            }

            RealApplicationType = applicationType;
            return this;
        }

        public StandaloneKestrelServerOptions Configure(IConfiguration configuration, bool reloadOnChange)
        {
            ConfigurationLoader = new StandaloneKestrelServerConfigurationLoader(configuration, reloadOnChange);
            ConfigureKestrel(configuration.GetSection("Kestrel"), reloadOnChange);
            return this;
        }

        internal StandaloneKestrelServerOptions ConfigureKestrel(IConfigurationSection? config, bool reloadOnChange)
        {
            if (config.Exists())
            {
                KestrelServerOptions.Configure(config, reloadOnChange);
            }

            return this;
        }
    }
}