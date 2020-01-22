using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Common;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class CalledServiceBaseUnitTests
    {
        [TestMethod]
        public void NullParametersNoCacheTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new CalledServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(new CalledServiceInfo(), "cacheRegion", null);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void NoParametersNoCacheTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new CalledServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(new CalledServiceInfo(), "cacheRegion", new KeyValuePair<string, string>[0]);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void NoParametersTest()
        {
            var cacheRegion = "cacheRegion";
            var userIdentifier = "User";
            var serviceInfo = new CalledServiceInfo
            {
                AdditionalParameters = new ParameterInfo[0]
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, Constants.UserIdentifierCacheKey).Returns(new CacheEntry<string> { Value = userIdentifier });

            var sut = new CalledServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(serviceInfo, cacheRegion, null);

            cache.Received(1).Get<string>(cacheRegion, Constants.UserIdentifierCacheKey);
            IEnumerable<KeyValuePair<string, string>> keyValuePairs = actual as KeyValuePair<string, string>[] ?? actual.ToArray();
            Assert.IsTrue(keyValuePairs.Any());
            Assert.AreEqual(1, keyValuePairs.Count());

            KeyValuePair<string, string> parameter = keyValuePairs.First();

            Assert.AreEqual(Constants.UserIdentifierPostKey, parameter.Key);
            Assert.AreEqual(userIdentifier, parameter.Value);
        }

        [TestMethod]
        public void AdditionalParametersTest()
        {
            var cacheRegion = "cacheRegion";
            var userIdentifier = "User";
            Guid serviceParameterId = Guid.NewGuid();
            Guid nonExistingServiceParameterId = Guid.NewGuid();
            var serviceParameterKey = "Key";
            var serviceParameterValue = "Value";
            var serviceInfo = new CalledServiceInfo
            {
                AdditionalParameters = new[]
                {
                    new ParameterInfo
                    {
                        TokenId = serviceParameterId,
                        Name = serviceParameterKey
                    },
                    new ParameterInfo
                    {
                        TokenId = nonExistingServiceParameterId,
                        Name = serviceParameterKey
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, Constants.UserIdentifierCacheKey).Returns(new CacheEntry<string> { Value = userIdentifier });
            cache.Get<string>(cacheRegion, serviceParameterId.ToString()).Returns(new CacheEntry<string> { Value = serviceParameterValue });

            var sut = new CalledServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(serviceInfo, cacheRegion, null);

            cache.Received(1).Get<string>(cacheRegion, Constants.UserIdentifierCacheKey);
            IEnumerable<KeyValuePair<string, string>> keyValuePairs = actual as KeyValuePair<string, string>[] ?? actual.ToArray();
            Assert.IsTrue(keyValuePairs.Any());
            Assert.AreEqual(2, keyValuePairs.Count());

            KeyValuePair<string, string> parameter = keyValuePairs.First();

            Assert.AreEqual(Constants.UserIdentifierPostKey, parameter.Key);
            Assert.AreEqual(userIdentifier, parameter.Value);

            parameter = keyValuePairs.Skip(1).First();

            Assert.AreEqual(serviceParameterKey, parameter.Key);
            Assert.AreEqual(serviceParameterValue, parameter.Value);
        }

        [TestMethod]
        public async Task CallServiceBadServiceTypeTest()
        {
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo();

            var cache = Substitute.For<ICache>();

            var sut = new CalledServiceBaseInstance(cache);

            var actual = await sut.CallService(serviceInfo, cacheRegion, CancellationToken.None, new KeyValuePair<string, string>[0]);

            Assert.IsNull(actual);
        }

        private class CalledServiceBaseInstance : CalledServiceBase
        {
            public CalledServiceBaseInstance(ICache cache) : base(cache)
            {
            }

            protected override Task<ServiceResponse> CallServiceInternal(CalledServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
            {
                return Task.FromResult(new ServiceResponse());
            }
        }
    }
}
