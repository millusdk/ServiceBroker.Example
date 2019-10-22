using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class DynamicServiceUnitTests
    {
        [TestMethod]
        public async Task NullServiceTest()
        {
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(null, cacheRegion, CancellationToken.None, null);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public async Task NullEndpointTest()
        {
            var serviceInfo = new ServiceInfo();
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public async Task ServiceTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                    BreakDuration = TimeSpan.FromMilliseconds(1)
                },
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Success, actual.Status);
        }

        [TestMethod]
        public async Task ServiceExceptionTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                    BreakDuration = TimeSpan.FromMilliseconds(1)
                }
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ThrowsForAnyArgs(new Exception());

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Error, actual.Status);
        }

        [TestMethod]
        public async Task ServiceTimeoutTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                    BreakDuration = TimeSpan.FromMilliseconds(1)
                },
                Timeout = TimeSpan.FromMilliseconds(1)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(async info =>
            {
                await Task.Delay(200);
                return httpClientResponse;
            });

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Timeout, actual.Status);
        }

        [TestMethod]
        public async Task ServiceCircuitBreakerTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                    BreakDuration = TimeSpan.FromSeconds(1000)
                },
                Timeout = TimeSpan.FromSeconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.InternalServerError,
                Response = ""
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Error, actual.Status);

            await Task.Delay(10);

            actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            await httpClientWrapper.ReceivedWithAnyArgs(1).PostAsync(serviceInfo.Endpoint, null, CancellationToken.None);

            Assert.AreEqual(ServiceResponseStatus.Error, actual.Status);
        }

        [TestMethod]
        public async Task CancelledServiceTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                    BreakDuration = TimeSpan.FromMilliseconds(1)
                },
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };
            var cancellationToken = new CancellationToken(true);

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new DynamicService(cache, httpClientWrapper, tokenService);


            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, cancellationToken, null);

            Assert.AreEqual(ServiceResponseStatus.Error, actual.Status);
        }
    }
}