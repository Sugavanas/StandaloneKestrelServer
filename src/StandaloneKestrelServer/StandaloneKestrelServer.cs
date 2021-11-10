using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StandaloneKestrelServer
{
    public class StandaloneKestrelServer : KestrelServer
    {
        [ActivatorUtilitiesConstructor]
        public StandaloneKestrelServer(IOptions<StandaloneKestrelServerOptions> standaloneKestrelServerOptions,
            ILoggerFactory loggerFactory)
            : base(GetKestrelServerOptions(standaloneKestrelServerOptions.Value), GetTransportFactory(loggerFactory),
                loggerFactory)
        {
        }
        
        public StandaloneKestrelServer(IOptions<KestrelServerOptions> options,
            IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory)
            : base(options, transportFactory, loggerFactory)
        {
        }
        
        protected static IOptions<KestrelServerOptions> GetKestrelServerOptions(
            StandaloneKestrelServerOptions standaloneKestrelServerOptions)
        {
            return new OptionsWrapper<KestrelServerOptions>(standaloneKestrelServerOptions.KestrelServerOptions);
        }

        protected static IConnectionListenerFactory GetTransportFactory(ILoggerFactory loggerFactory)
        {
            //TODO: Allow this to be configurable / Use DI.
            var transportOptions = new SocketTransportOptions();
            return new SocketTransportFactory(new OptionsWrapper<SocketTransportOptions>(transportOptions),
                loggerFactory);
        }
    }
}