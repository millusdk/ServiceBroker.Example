using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Mocks;
using ServiceBroker.Example.Models;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class CachedServiceUnitTests
    {
        [TestMethod]
        public async Task ServiceTest()
        {
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                ExceptionCount = 1,
                BreakDuration = TimeSpan.FromMilliseconds(1),
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new CachedService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Success, actual.Status);
        }

        [TestMethod]
        public async Task CachingTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                ExceptionCount = 1,
                BreakDuration = TimeSpan.FromMilliseconds(1),
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };

            var cache = new CacheMock();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new CachedService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Success, actual.Status);

            actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.AreEqual(ServiceResponseStatus.Success, actual.Status);

            await httpClientWrapper.ReceivedWithAnyArgs(1).PostAsync(serviceInfo.Endpoint, null, CancellationToken.None);

            var semaphoresRemoved = sut.RemoveSemaphores(cacheRegion);

            Assert.IsTrue(semaphoresRemoved);
        }

        [TestMethod]
        public async Task SemaphoreTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                ExceptionCount = 1,
                BreakDuration = TimeSpan.FromMilliseconds(1),
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };

            var cache = new CacheMock();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(async callInfo =>
            {
                await Task.Delay(20);
                return httpClientResponse;
            });

            var sut = new CachedService(cache, httpClientWrapper, tokenService);

            Task<ServiceResponse> task1 = Task.Run(async () => await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null));

            Task<ServiceResponse> task2 = Task.Run(async () => await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null));

            await Task.WhenAll(task1, task2);

            await httpClientWrapper.ReceivedWithAnyArgs(1).PostAsync(serviceInfo.Endpoint, null, CancellationToken.None);
        }

        [TestMethod]
        public async Task CancelledServiceTest()
        {
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Endpoint = "Endpoint",
                ExceptionCount = 1,
                BreakDuration = TimeSpan.FromMilliseconds(1),
                Timeout = TimeSpan.FromMilliseconds(100)
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent,
                Response = ""
            };
            var cacheRegion = "cacheRegion";
            var cancellationToken = new CancellationToken(true);

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var tokenService = Substitute.For<ITokenService>();

            httpClientWrapper.PostAsync(serviceInfo.Endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var sut = new CachedService(cache, httpClientWrapper, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, cancellationToken, null);

            Assert.AreEqual(ServiceResponseStatus.Timeout, actual.Status);
        }
    }
}