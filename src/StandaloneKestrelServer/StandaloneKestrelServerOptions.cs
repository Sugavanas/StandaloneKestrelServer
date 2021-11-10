using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace StandaloneKestrelServer
{
    public class StandaloneKestrelServerOptions
    {
        public string Name { get; set; } = "StandaloneKestrelServer";

        public virtual KestrelServerOptions KestrelServerOptions { get; set; } = new KestrelServerOptions();

        public virtual Type ServerType
        {
            get => _serverType;
            set => UseServer(value);
        }

        public virtual Action<IApplicationBuilder> RequestPipeline { get; set; } = default;

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

        public virtual StandaloneKestrelServerOptions UseServer(Type serverType)
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
    }
}