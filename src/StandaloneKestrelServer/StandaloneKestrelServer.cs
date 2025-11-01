using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TS.StandaloneKestrelServer
{
    public class StandaloneKestrelServer : KestrelServer
    {
        [ActivatorUtilitiesConstructor]
        public StandaloneKestrelServer(IOptions<StandaloneKestrelServerOptions> standaloneKestrelServerOptions,
            IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory)
            : base(GetKestrelServerOptions(standaloneKestrelServerOptions.Value), transportFactory,
                loggerFactory)
        {
        }
        
        public StandaloneKestrelServer(IOptions<StandaloneKestrelServerOptions> standaloneKestrelServerOptions,
            IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory,
            object? dummy = null) // Additional parameter added to work around .NET8.0 bug with ActivatorUtilitiesConstructor: https://github.com/dotnet/runtime/issues/95915
            : this(standaloneKestrelServerOptions, transportFactory, loggerFactory)
        {
        }

        [Obsolete("If KestrelServerOptions needs to be passed in directly, use KestrelServer class directly")]
        public StandaloneKestrelServer(IOptions<KestrelServerOptions> options,
            IConnectionListenerFactory transportFactory, 
            ILoggerFactory loggerFactory
            )
            : base(options, transportFactory, loggerFactory)
        {
        }

        protected static IOptions<KestrelServerOptions> GetKestrelServerOptions(
            StandaloneKestrelServerOptions standaloneKestrelServerOptions)
        {
            return new OptionsWrapper<KestrelServerOptions>(standaloneKestrelServerOptions.KestrelServerOptions);
        }
    }
}