using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace StandaloneKestrelServer
{
    public class StandaloneKestrelServerOptions
    {
        public string Name { get; set; } = "StandaloneKestrelServer";

        public virtual KestrelServerOptions KestrelServerOptions { get; set; } = new KestrelServerOptions();

        public virtual Type ServerType { get; set; } = typeof(StandaloneKestrelServer);

        public virtual Action<ApplicationBuilder> RequestPipeline { get; set; } = default;
    }
}