using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;

namespace TS.StandaloneKestrelServer
{
    /// <summary>
    /// Reimplementation of <see cref="Microsoft.AspNetCore.Http.Features.FeatureCollection"/> with some minor changes.
    /// Licensed under MIT License: https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt
    /// </summary>
    [Obsolete("Use HttpContext.Features directly.")]
    public class PersistentContainer : FeatureCollection, IFeatureCollection
    {
        public PersistentContainer()
        {
        }
    }
}