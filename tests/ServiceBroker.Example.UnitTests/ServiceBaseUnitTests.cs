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
    public class ServiceBaseUnitTests
    {
        [TestMethod]
        public void NullParametersNoCacheTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new ServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(new ServiceInfo(), "cacheRegion", null);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void NoParametersNoCacheTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new ServiceBaseInstance(cache);

            IEnumerable<KeyValuePair<string, string>> actual = sut.GetPostParameters(new ServiceInfo(), "cacheRegion", new KeyValuePair<string, string>[0]);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void NoParametersTest()
        {
            var cacheRegion = "cacheRegion";
            var userIdentifier = "User";
            var serviceInfo = new ServiceInfo
            {
                AdditionalParameters = new ParameterInfo[0]
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, Constants.UserIdentifierCacheKey).Returns(new CacheEntry<string> { Value = userIdentifier });

            var sut = new ServiceBaseInstance(cache);

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
            var serviceInfo = new ServiceInfo
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

            var sut = new ServiceBaseInstance(cache);

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

        private class ServiceBaseInstance : ServiceBase
        {
            public ServiceBaseInstance(ICache cache) : base(cache)
            {
            }

            protected override Task<ServiceResponse> CallServiceInternal(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
            {
                throw new NotImplementedException();
            }
        }
    }
}
