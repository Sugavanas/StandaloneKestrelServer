using System;
using Microsoft.Extensions.Options;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServerOptionsSetup : IConfigureOptions<StandaloneKestrelServerOptions>
    {
        private readonly IServiceProvider _services;

        public StandaloneKestrelServerOptionsSetup(IServiceProvider services)
        {
            _services = services;
        }

        public void Configure(StandaloneKestrelServerOptions options)
        {
            options.KestrelServerOptions.ApplicationServices = _services;
        }
    }
}