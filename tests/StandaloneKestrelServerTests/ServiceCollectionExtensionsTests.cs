using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TS.StandaloneKestrelServer;
using TS.StandaloneKestrelServer.Extensions;
using Xunit;

namespace StandaloneKestrelServerTests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void EnsureConfigureStandaloneKestrelServerCallsOptionsConfigureMethod()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["StandaloneKestrel:Name"] = "SampleServer",
                // Ensure Kestrel section passes as null. This should be fine as we are just checking if ConfigurationLoader is set.
                // Otherwise, we will need to load all services required by Kestrel (e.g. Microsoft.Extensions.Hosting.IHostEnvironment) 
                ["StandaloneKestrel:Kestrel"] = null
                // ["StandaloneKestrel:Kestrel:Endpoints:http:Url"] = "http://localhost:8100"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandaloneKestrelServerServices();

            serviceCollection.ConfigureStandaloneKestrelServer(configuration.GetSection("StandaloneKestrel"));

            var services = serviceCollection.BuildServiceProvider();
            var options = services.GetService<IOptions<StandaloneKestrelServerOptions>>();

            Assert.NotNull(options);
            Assert.NotNull(options.Value.ConfigurationLoader);
        }
    }
}