using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TS.StandaloneKestrelServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStandaloneKestrelServerServices(this IServiceCollection service)
        {
            service.AddHostedService<StandaloneKestrelServerService>();

            service.AddOptions<StandaloneKestrelServerOptions>();
            service
                .AddTransient<IConfigureOptions<StandaloneKestrelServerOptions>, StandaloneKestrelServerOptionsSetup>();

            var listener = new DiagnosticListener("TS.StandaloneKestrelServer");
            service.AddSingleton<DiagnosticListener>(listener);
            service.AddSingleton<DiagnosticSource>(listener);

            // The following is required to add kestrel specific services which are currently internal
            // https://github.com/dotnet/aspnetcore/issues/48956
            var dummyServiceCollection = new ServiceCollection();
            var dummyBuilder = new DummyWebHostBuilder(dummyServiceCollection);
            dummyBuilder.UseKestrelCore();
            
            var kestrelServices = dummyServiceCollection.Where(sd =>
                sd.ServiceType == typeof(IConnectionListenerFactory) ||
                sd.ServiceType.FullName?.Contains("IHttpsConfigurationService") == true);

            foreach (var serviceDescriptor in kestrelServices)
            {
                service.Add(serviceDescriptor);
            }
            
            return service;
        }

        private class DummyWebHostBuilder : IWebHostBuilder
        {
            private readonly IServiceCollection _serviceCollection;

            public DummyWebHostBuilder(IServiceCollection serviceCollection)
            {
                _serviceCollection = serviceCollection;
            }
            
            public IWebHost Build() => throw new NotImplementedException();


            public IWebHostBuilder ConfigureAppConfiguration(
                Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate) =>
                throw new NotImplementedException();

            public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
            {
                configureServices(_serviceCollection);
                return this;
            }

            public IWebHostBuilder ConfigureServices(
                Action<WebHostBuilderContext, IServiceCollection> configureServices)=> throw new NotImplementedException(); 
            public string? GetSetting(string key) => throw new NotImplementedException();

            public IWebHostBuilder UseSetting(string key, string? value) => throw new NotImplementedException();
        }
    }
}