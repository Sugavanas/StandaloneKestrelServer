using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace TS.StandaloneKestrelServer
{
    /// <summary>
    /// Reimplementation of <see cref="Microsoft.AspNetCore.Hosting.HostingApplication"/> with some minor changes.
    /// Licensed under MIT License: https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt
    /// </summary>
    public class Application : IHttpApplication<Application.Context>
    {
        private readonly RequestDelegate _requestPipeline;

        private readonly ILogger _logger;

        public Application(RequestDelegate requestPipeline, ILoggerFactory loggerFactory)
        {
            _requestPipeline = requestPipeline;
            _logger = loggerFactory.CreateLogger<Application>();
        }

        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            Context? context;
            //Refer to AspNet Core Application class.
            if (contextFeatures is IHostContextContainer<Context> container)
            {
                context = container.HostContext;
                if (context is null)
                {
                    var httpContext = new DefaultHttpContext(contextFeatures);
                    context = new Context(httpContext);
                    container.HostContext = context;
                }
                else
                {
                    if (context.HttpContext is DefaultHttpContext httpContext)
                    {
                        httpContext.Initialize(contextFeatures);
                    }
                    else
                    {
                        context.HttpContext = new DefaultHttpContext(contextFeatures);
                    }
                }
            }
            else
            {
                var httpContext = new DefaultHttpContext(contextFeatures);
                context = new Context(httpContext);
            }

            context.HttpContext.Features.Set(context.Container);
            return context;
        }

        public async Task ProcessRequestAsync(Context context)
        {
            _logger.LogDebug("ProcessRequestAsync: Started");
            await _requestPipeline(context.HttpContext);
            _logger.LogDebug("ProcessRequestAsync: Done");
        }

        public void DisposeContext(Context context, Exception? exception)
        {
            ((DefaultHttpContext) context.HttpContext).Uninitialize();
        }

        public class Context
        {
            public HttpContext HttpContext { get; set; }

            public PersistentContainer Container { get; } = new();

            public Context(HttpContext httpContext)
            {
                HttpContext = httpContext;
            }
        }
    }
}