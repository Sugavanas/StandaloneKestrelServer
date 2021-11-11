using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TS.StandaloneKestrelServer;

namespace SampleServer
{
    public class HttpServer : TS.StandaloneKestrelServer.StandaloneKestrelServer
    {
        public HttpServer(IOptions<StandaloneKestrelServerOptions> standaloneKestrelServerOptions, ILoggerFactory loggerFactory) : base(standaloneKestrelServerOptions, loggerFactory)
        {
        }
        
        public HttpServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory) : base(options, transportFactory, loggerFactory)
        {
        }
    }
}