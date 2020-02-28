using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class StaticServiceUnitTests
    {
        [TestMethod]
        public async Task ServiceTest()
        {
            var tokenId = Guid.NewGuid();

            var serviceInfo = new StaticServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
                Data = "Data",
                Tokens = new[] {
                    new TokenInfo
                    {
                        Id = tokenId
                    }
                }
            };
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();
            var tokenService = Substitute.For<ITokenService>();

            var sut = new StaticService(cache, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            tokenService.Received(1).ParseTokens(cacheRegion, serviceInfo.Data, serviceInfo.Tokens);

            Assert.AreEqual(ServiceResponseStatus.Success, actual.Status);
            Assert.AreEqual(serviceInfo.Data, actual.Value);
        }

        [TestMethod]
        public async Task WrongServiceTypeTest()
        {
            var serviceInfo = new ServiceInfo
            {
                Name = "Service",
                Id = Guid.NewGuid(),
            };
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();
            var tokenService = Substitute.For<ITokenService>();

            var sut = new StaticService(cache, tokenService);

            ServiceResponse actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, null);

            Assert.IsNull(actual);
        }
    }
}