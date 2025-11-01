using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TS.StandaloneKestrelServer;
using TS.StandaloneKestrelServer.Extensions;
using Xunit;

namespace StandaloneKestrelServerTests
{
    public class ApplicationTests
    {
        [Fact]
        public void TestCreateContext()
        {
            var featureCollection = new FeatureCollection();
            var (mockHttpContextFactory, mockHttpContext) =
                CreateMockHttpFactoryWithReturnableFeatures(featureCollection);

            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(),
                mockHttpContextFactory.Object);

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);
            Assert.Equal(mockHttpContext.Object, context.HttpContext);
        }

        [Fact]
        public void TestCreateContextWithNullFactory()
        {
            var mockHttpContextFactory = new Mock<IHttpContextFactory>();

            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(), null!);

            var featureCollection = new FeatureCollection();

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);
            Assert.NotNull(context.HttpContext);
            
            Assert.IsType<DefaultHttpContext>(context.HttpContext);
            
            Assert.Equal(featureCollection, context.HttpContext.Features);
        }

        [Fact]
        public void TestCreateContextIsStoredInHostContextContainer()
        {
            var featureCollection = new FeatureCollectionWithHostContextContainer();
            var (mockHttpContextFactory, mockHttpContext) =
                CreateMockHttpFactoryWithReturnableFeatures(featureCollection);

            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(),
                mockHttpContextFactory.Object);

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);
            Assert.Equal(mockHttpContext.Object, context.HttpContext);

            Assert.Equal(featureCollection, context.HttpContext.Features);
            Assert.Equal(context, featureCollection.HostContext);
        }

        [Fact]
        public void TestCreateContextIsStoredInHostContextContainerWithNullFactory()
        {
            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(), null!);

            var featureCollection = new FeatureCollectionWithHostContextContainer();

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);
            Assert.NotNull(context.HttpContext);
  
            Assert.Equal(featureCollection, context.HttpContext.Features);
            Assert.Equal(context, featureCollection.HostContext);
        }
        
        [Fact]
        public void TestCreateContextReusesContextFromHostContextContainer()
        {
            var featureCollection = new FeatureCollectionWithHostContextContainer();
            var (mockHttpContextFactory, mockHttpContext) =
                CreateMockHttpFactoryWithReturnableFeatures(featureCollection);

            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(),
                mockHttpContextFactory.Object);

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);
            mockHttpContextFactory.Verify(f => f.Create(featureCollection), Times.Once);
            mockHttpContextFactory.Invocations.Clear();

            var context2 = application.CreateContext(featureCollection);

            Assert.Equal(context, context2);
            mockHttpContextFactory.Verify(f => f.Create(featureCollection), Times.Never);
            //TODO: test if initialize was called
        }

        [Fact]
        public void TestCreateContextReusesContextFromHostContextContainerWithNullFactory()
        {
            var application = new Application((_) => Task.CompletedTask, new NullLoggerFactory(), null!);

            var featureCollection = new FeatureCollectionWithHostContextContainer();

            var context = application.CreateContext(featureCollection);

            Assert.NotNull(context);

            var context2 = application.CreateContext(featureCollection);

            Assert.Equal(context, context2);
        }

        [Fact]
        public async Task TestProcessRequestAsync()
        {
            var testValue = 0;
            RequestDelegate requestDelegate = httpContext =>
            {
                testValue++;
                return Task.CompletedTask;
            };

            var application = new Application(requestDelegate, new NullLoggerFactory(), null!);
            var context = application.CreateContext(new FeatureCollection());

            await application.ProcessRequestAsync(context);

            Assert.Equal(1, testValue);
        }


        private (Mock<IHttpContextFactory> mockHttpContextFactory, Mock<HttpContext> mockHttpContext)
            CreateMockHttpFactoryWithReturnableFeatures(IFeatureCollection featureCollection)
        {
            var mockHttpContext = new Mock<HttpContext>();
            var mockHttpContextFactory = new Mock<IHttpContextFactory>();

            mockHttpContext.SetupGet(c => c.Features).Returns(featureCollection);
            mockHttpContextFactory.Setup(f => f.Create(featureCollection)).Returns(mockHttpContext.Object);

            return (mockHttpContextFactory, mockHttpContext);
        }

        private class FeatureCollectionWithHostContextContainer : IFeatureCollection,
            IHostContextContainer<Application.Context>
        {
            public object? this[Type key]
            {
                get => _featureCollection[key];
                set => _featureCollection[key] = value;
            }

#pragma warning disable CS8766 //net5.0
            public Application.Context? HostContext { get; set; } = null;
#pragma warning restore CS8766

            public bool IsReadOnly => false;

            public int Revision => 0;

            private readonly IFeatureCollection _featureCollection = new FeatureCollection();

#pragma warning disable CS8766 //net 5.0
            public TFeature? Get<TFeature>()
            {
                return _featureCollection.Get<TFeature>();
            }
#pragma warning restore CS8766

            public void Set<TFeature>(TFeature? instance)
            {
                _featureCollection.Set(instance);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
            {
                return _featureCollection.GetEnumerator();
            }
        }
    }
}