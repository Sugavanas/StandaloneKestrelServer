using System;
using Microsoft.AspNetCore.Http;

namespace TS.StandaloneKestrelServer.Extensions
{
    public static class HttpContextExtensions
    {
#nullable enable
        [Obsolete("Use HttpContext.Features directly.")]
        public static PersistentContainer? GetPersistentContainer(this HttpContext httpContext)
        {
            return httpContext.Features.Get<PersistentContainer>();
        }
#nullable disable
    }
}