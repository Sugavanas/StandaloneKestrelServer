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
            Context context;
            //Refer to AspNet Core Application class.
            if (contextFeatures is IHostContextContainer<Application.Context> container)
            {
                context = container.HostContext;
                if (context is null)
                {
                    context = new Application.Context();
                    container.HostContext = context;
                }
            }
            else
            {
                context = new Application.Context();
            }

            DefaultHttpContext httpContext = (DefaultHttpContext) context.HttpContext;

            if (httpContext is null)
                context.HttpContext = new DefaultHttpContext(contextFeatures);
            else
                httpContext.Initialize(contextFeatures);

            context.Container ??= new PersistentContainer();

            context.HttpContext.Features.Set(context.Container);
            return context;
        }

        public async Task ProcessRequestAsync(Context context)
        {
            _logger.LogDebug("ProcessRequestAsync: Started");
            await _requestPipeline(context.HttpContext);
            _logger.LogDebug("ProcessRequestAsync: Done");
        }

        public void DisposeContext(Context context, Exception exception)
        {
            ((DefaultHttpContext) context.HttpContext).Uninitialize();
        }

        public class Context
        {
            public HttpContext HttpContext { get; set; }

            public PersistentContainer Container { get; set; }
        }
    }
}