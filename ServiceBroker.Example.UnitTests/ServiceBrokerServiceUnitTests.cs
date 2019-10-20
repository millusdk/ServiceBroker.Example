﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Mocks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class ServiceBrokerServiceUnitTests
    {
        [TestMethod]
        public async Task EmptyListAsyncTest()
        {
            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();
            
            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = await sut.CallServicesAsync(new Guid[0], "region", TimeSpan.Zero);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public async Task NullListAsyncTest()
        {
            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = await sut.CallServicesAsync(null, "region", TimeSpan.Zero);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public async Task TimeoutServiceAsyncTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.Run(async () =>
                {
                    await Task.Delay(10);
                    return new ServiceResponse();
                }));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = await sut.CallServicesAsync(serviceAndTokenIds, cacheRegion, TimeSpan.Zero);

            serviceRepository.Received(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Timeout, dynamicServiceResponse.Status);
            Assert.IsNull(dynamicServiceResponse.Value);
            Assert.IsFalse(dynamicServiceResponse.TokenResponses.Any());
        }

        [TestMethod]
        public async Task FailedServiceAsyncTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.FromException<ServiceResponse>(new Exception()));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = await sut.CallServicesAsync(serviceAndTokenIds, cacheRegion, TimeSpan.Zero);

            serviceRepository.Received(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Error, dynamicServiceResponse.Status);
            Assert.IsNull(dynamicServiceResponse.Value);
            Assert.IsFalse(dynamicServiceResponse.TokenResponses.Any());
        }

        [TestMethod]
        public async Task DynamicServiceAsyncTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";
            var serviceResponse = "Service response";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.FromResult(new ServiceResponse
                {
                    Value = serviceResponse
                }));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = await sut.CallServicesAsync(serviceAndTokenIds, cacheRegion, TimeSpan.FromMilliseconds(10));

            serviceRepository.Received(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, dynamicServiceResponse.Status);
            Assert.AreEqual(serviceResponse, dynamicServiceResponse.Value);
        }

        [TestMethod]
        public void EmptyListTest()
        {
            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(new Guid[0], "region", TimeSpan.Zero);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void NullListTest()
        {
            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(null, "region", TimeSpan.Zero);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TimeoutServiceTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.Run(async () =>
                {
                    await Task.Delay(10);
                    return new ServiceResponse();
                }));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(serviceAndTokenIds, cacheRegion, TimeSpan.Zero);

            serviceRepository.Received(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Timeout, dynamicServiceResponse.Status);
            Assert.IsNull(dynamicServiceResponse.Value);
            Assert.IsFalse(dynamicServiceResponse.TokenResponses.Any());
        }

        [TestMethod]
        public void FailedServiceTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.FromException<ServiceResponse>(new Exception()));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(serviceAndTokenIds, cacheRegion, TimeSpan.Zero);

            serviceRepository.Received(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Error, dynamicServiceResponse.Status);
            Assert.IsNull(dynamicServiceResponse.Value);
            Assert.IsFalse(dynamicServiceResponse.TokenResponses.Any());
        }

        [TestMethod]
        public void DynamicServiceTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            var serviceAndTokenIds = new List<Guid> { serviceGuid };
            var cacheRegion = "region";
            var serviceResponse = "Service response";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).ReturnsForAnyArgs(new[] { serviceInfo });
            dynamicService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.FromResult(new ServiceResponse
                {
                    Value = serviceResponse
                }));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(serviceAndTokenIds, cacheRegion, TimeSpan.FromMilliseconds(10));

            serviceRepository.ReceivedWithAnyArgs(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, dynamicServiceResponse.Status);
            Assert.AreEqual(serviceResponse, dynamicServiceResponse.Value);
        }

        [TestMethod]
        public void CachedServiceTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new CachedServiceInfo
            {
                Id = serviceGuid,
            };
            var serviceAndTokenIds = new List<Guid> { serviceGuid };
            var cacheRegion = "region";
            var serviceResponse = "Service response";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).ReturnsForAnyArgs(new[] { serviceInfo });
            cachedService.CallService(serviceInfo, cacheRegion, CancellationToken.None, null)
                .ReturnsForAnyArgs(Task.FromResult(new ServiceResponse
                {
                    Value = serviceResponse
                }));

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            ServiceBrokerResponse actual = sut.CallServices(serviceAndTokenIds, cacheRegion, TimeSpan.FromMilliseconds(10));

            serviceRepository.ReceivedWithAnyArgs(1).GetServicesAndTokens(serviceAndTokenIds);

            Assert.AreEqual(1, actual.ServiceResponses.Count());

            ServiceResponse dynamicServiceResponse = actual.ServiceResponses.First();

            Assert.AreEqual(ServiceResponseStatus.Success, dynamicServiceResponse.Status);
            Assert.AreEqual(serviceResponse, dynamicServiceResponse.Value);
        }

        [TestMethod]
        public void StartBackgroundServiceCallsTest()
        {
            Guid serviceGuid = Guid.NewGuid();
            var serviceInfo = new DynamicServiceInfo
            {
                Id = serviceGuid,
            };
            Guid[] serviceAndTokenIds = new[] { serviceGuid };
            var cacheRegion = "region";

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = new TaskSchedulerMock();
            var cache = Substitute.For<ICache>();

            serviceRepository.GetServicesAndTokens(serviceAndTokenIds).Returns(new[] { serviceInfo });

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            sut.StartBackgroundServiceCalls(serviceAndTokenIds, cacheRegion, TimeSpan.FromSeconds(10));

            dynamicService.ReceivedWithAnyArgs(1).CallService(serviceInfo, cacheRegion, CancellationToken.None, null);
        }


        [TestMethod]
        public void GetUserProfileTest()
        {
            Guid service1Id = Guid.NewGuid();
            Guid service2Id = Guid.NewGuid();
            Guid token1Id = Guid.NewGuid();
            Guid token2Id = Guid.NewGuid();
            var service1Name = "Service1";
            var service2Name = "Service2";
            var token1Name = "Token1";
            var token2Name = "Token1";
            var token1Content = "content";
            var cachedService1Info = new CachedServiceInfo
            {
                Id = service1Id,
                Name = service1Name,
                Tokens = new []
                {
                    new XPathTokenInfo
                    {
                        Id = token1Id,
                        Name = token1Name,
                        XPath = "/node/text()"
                    },
                    new XPathTokenInfo
                    {
                        Id = token2Id,
                        Name = token2Name,
                        XPath = "/node/text()"
                    }
                }
            };
            var cachedService2Info = new CachedServiceInfo
            {
                Id = service2Id,
                Name = service2Name,
                Tokens = new TokenInfo[0]
            };
            var cacheRegion = "region";
            var serviceCacheEntry = new CacheEntry<string>
            {
                Value = $"<node>{token1Content}</node>"
            };
            var tokenCacheEntry = new CacheEntry<string>
            {
                Value = token1Content
            };

            var serviceRepository = Substitute.For<IServiceRepository>();
            var dynamicService = Substitute.For<IDynamicService>();
            var cachedService = Substitute.For<ICachedService>();
            var taskScheduler = Substitute.For<ITaskScheduler>();
            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, service1Id.ToString()).Returns(serviceCacheEntry);
            cache.Get<string>(cacheRegion, token1Id.ToString()).Returns(tokenCacheEntry);

            serviceRepository.GetCachedServices().ReturnsForAnyArgs(new[] { cachedService1Info,  cachedService2Info });

            var sut = new ServiceBrokerService(serviceRepository, dynamicService, cachedService, taskScheduler, cache);

            XDocument profile = sut.GetUserProfile(cacheRegion);

            Assert.IsNotNull(profile.Root);

            XNode serviceNode = profile.Root.FirstNode;

            Assert.IsNotNull(serviceNode);

            Assert.AreEqual(XmlNodeType.Element, serviceNode.NodeType);

            var serviceElement = serviceNode as XElement;
            Assert.IsNotNull(serviceElement);

            Assert.AreEqual(service1Name, serviceElement.Name);
        }
    }
}
