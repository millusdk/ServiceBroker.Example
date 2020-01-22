using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Mocks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.FunctionalTests
{
    [TestClass]
    public class ServiceBrokerServiceFunctionalTests
    {
        [TestMethod]
        public void SingleDynamicServiceTest()
        {
            Guid serviceId = Guid.NewGuid();
            Guid tokenId = Guid.NewGuid();
            var cacheRegion = "cacheRegion";
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(10);
            Guid[] serviceIds = new[] { serviceId };
            var endpoint = "/";
            var innerText = "Text";
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceId,
                Endpoint = endpoint,
                Timeout = TimeSpan.FromMilliseconds(5),
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1,
                },
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = tokenId,
                        Name = "XPath",
                        XPath = "/content/text()"
                    }
                }
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Response = $"<content>{innerText}</content>"
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var serviceRepository = Substitute.For<IServiceRepository>();
            var taskScheduler = Substitute.For<ITaskScheduler>();

            serviceRepository.GetServicesAndTokens(serviceIds).Returns(new[] { serviceInfo });
            httpClientWrapper.PostAsync(endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var tokenService = new TokenService(cache);
            var dynamicService = new DynamicService(cache, httpClientWrapper, tokenService);
            var cachedService = new CachedService(cache, httpClientWrapper, tokenService);
            var staticService = new StaticService(cache, tokenService);
            var serviceBroker = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, staticService, taskScheduler, cache);

            ServiceBrokerResponse actual = serviceBroker.CallServices(serviceIds, cacheRegion, timeSpan);

            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.ServiceResponses);
            Assert.IsTrue(actual.ServiceResponses.Any());

            ServiceResponse serviceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, serviceResponse.Status);
            Assert.AreEqual(httpClientResponse.Response, serviceResponse.Value);
            Assert.IsNotNull(serviceResponse.TokenResponses);
            Assert.IsTrue(serviceResponse.TokenResponses.Any());

            TokenResponse tokenResponse = serviceResponse.TokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse.Status);
            Assert.AreEqual(innerText, tokenResponse.Value);
        }

        [TestMethod]
        public void MultipleDynamicServiceTest()
        {
            Guid service1Id = Guid.NewGuid();
            Guid service2Id = Guid.NewGuid();
            Guid token1Id = Guid.NewGuid();
            Guid token2Id = Guid.NewGuid();
            var cacheRegion = "cacheRegion";
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(10);
            Guid[] serviceIds = new[] { service1Id, service2Id };
            var endpoint1 = "/endpoint1";
            var endpoint2 = "/endpoint2";
            var innerText1 = "Text1";
            var innerText2 = "Text2";
            var service1Info = new DynamicServiceInfo
            {
                Id = service1Id,
                Endpoint = endpoint1,
                Timeout = TimeSpan.FromMilliseconds(5),
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1
                },
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = token1Id,
                        Name = "XPath",
                        XPath = "/content/text()"
                    }
                }
            };
            var service2Info = new DynamicServiceInfo
            {
                Id = service2Id,
                Endpoint = endpoint2,
                Timeout = TimeSpan.FromMilliseconds(5),
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1
                },
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = token2Id,
                        Name = "XPath",
                        XPath = "/content/text()"
                    }
                }
            };

            var httpClientResponse1 = new HttpClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Response = $"<content>{innerText1}</content>"
            };
            var httpClientResponse2 = new HttpClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Response = $"<content>{innerText2}</content>"
            };

            var cache = Substitute.For<ICache>();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var serviceRepository = Substitute.For<IServiceRepository>();
            var taskScheduler = Substitute.For<ITaskScheduler>();

            serviceRepository.GetServicesAndTokens(serviceIds).Returns(new[] { service1Info, service2Info });
            httpClientWrapper.PostAsync(endpoint1, null, CancellationToken.None).ReturnsForAnyArgs(callInfo =>
            {
                var endpointArg = callInfo.ArgAt<string>(0);

                if (endpointArg.Equals(endpoint1))
                {
                    return httpClientResponse1;
                }

                if (endpointArg.Equals(endpoint2))
                {
                    return httpClientResponse2;
                }

                return null;
            });

            var tokenService = new TokenService(cache);
            var dynamicService = new DynamicService(cache, httpClientWrapper, tokenService);
            var cachedService = new CachedService(cache, httpClientWrapper, tokenService);
            var staticService = new StaticService(cache, tokenService);
            var serviceBroker = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, staticService, taskScheduler, cache);

            ServiceBrokerResponse actual = serviceBroker.CallServices(serviceIds, cacheRegion, timeSpan);

            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.ServiceResponses);
            Assert.IsTrue(actual.ServiceResponses.Any());

            ServiceResponse serviceResponse1 = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, serviceResponse1.Status);
            Assert.AreEqual(httpClientResponse1.Response, serviceResponse1.Value);
            Assert.IsNotNull(serviceResponse1.TokenResponses);
            Assert.IsTrue(serviceResponse1.TokenResponses.Any());

            TokenResponse tokenResponse1 = serviceResponse1.TokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse1.Status);
            Assert.AreEqual(innerText1, tokenResponse1.Value);

            ServiceResponse serviceResponse2 = actual.ServiceResponses.Skip(1).First();

            Assert.AreEqual(ServiceResponseStatus.Success, serviceResponse2.Status);
            Assert.AreEqual(httpClientResponse2.Response, serviceResponse2.Value);
            Assert.IsNotNull(serviceResponse1.TokenResponses);
            Assert.IsTrue(serviceResponse1.TokenResponses.Any());

            TokenResponse tokenResponse2 = serviceResponse2.TokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse2.Status);
            Assert.AreEqual(innerText2, tokenResponse2.Value);
        }

        [TestMethod]
        public void SingleCachedServiceTest()
        {
            Guid serviceId = Guid.NewGuid();
            Guid tokenId = Guid.NewGuid();
            var cacheRegion = "cacheRegion";
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(10);
            Guid[] serviceIds = new[] { serviceId };
            var endpoint = "/";
            var innerText = "Text";
            var serviceInfo = new CachedServiceInfo
            {
                Id = serviceId,
                Endpoint = endpoint,
                Timeout = TimeSpan.FromMilliseconds(5),
                CircuitBreakerInfo = new CircuitBreakerInfo
                {
                    ExceptionCount = 1
                },
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = tokenId,
                        Name = "XPath",
                        XPath = "/content/text()"
                    }
                }
            };
            var httpClientResponse = new HttpClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Response = $"<content>{innerText}</content>"
            };

            var cache = new CacheMock();
            var httpClientWrapper = Substitute.For<IHttpClientWrapper>();
            var serviceRepository = Substitute.For<IServiceRepository>();
            var taskScheduler = Substitute.For<ITaskScheduler>();

            serviceRepository.GetServicesAndTokens(serviceIds).Returns(new[] { serviceInfo });
            httpClientWrapper.PostAsync(endpoint, null, CancellationToken.None).ReturnsForAnyArgs(httpClientResponse);

            var tokenService = new TokenService(cache);
            var dynamicService = new DynamicService(cache, httpClientWrapper, tokenService);
            var cachedService = new CachedService(cache, httpClientWrapper, tokenService);
            var staticService = new StaticService(cache, tokenService);
            var serviceBroker = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, staticService, taskScheduler, cache);

            ServiceBrokerResponse actual = serviceBroker.CallServices(serviceIds, cacheRegion, timeSpan);
            serviceBroker.CallServices(serviceIds, cacheRegion, timeSpan);

            httpClientWrapper.ReceivedWithAnyArgs(1).PostAsync(endpoint, null, CancellationToken.None);

            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.ServiceResponses);
            Assert.IsTrue(actual.ServiceResponses.Any());

            ServiceResponse serviceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, serviceResponse.Status);
            Assert.AreEqual(httpClientResponse.Response, serviceResponse.Value);
            Assert.IsNotNull(serviceResponse.TokenResponses);
            Assert.IsTrue(serviceResponse.TokenResponses.Any());

            TokenResponse tokenResponse = serviceResponse.TokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse.Status);
            Assert.AreEqual(innerText, tokenResponse.Value);
        }
    }
}
