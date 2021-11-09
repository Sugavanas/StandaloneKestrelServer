using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace StandaloneKestrelServer
{
    public class Application : IHttpApplication<Application.Context>
    {

        private readonly ILogger _logger;

        public Application(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Application>();
        }

        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            var context = new Application.Context
            {
                HttpContext = new DefaultHttpContext(contextFeatures)
            };
            return context;
        }

        public async Task ProcessRequestAsync(Context context)
        {
            _logger.LogDebug("ProcessRequestAsync: Started");
            await context.HttpContext.Response.WriteAsync("It Works!");
            _logger.LogDebug("ProcessRequestAsync: Done");
        }

        public void DisposeContext(Context context, Exception exception)
        {
            ((DefaultHttpContext) context.HttpContext).Uninitialize();
        }

        public class Context
        {
            public HttpContext HttpContext { get; set; }
        }
    }
}