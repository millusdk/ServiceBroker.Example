using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class TokenServiceUnitTests
    {

        #region ParseTokens

        [TestMethod]
        public void ParseTokensEmptyServiceResponseTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                IEnumerable<TokenResponse> _ = sut.ParseTokens("cacheRegion", "", null).ToArray();
            });
        }

        [TestMethod]
        public void ParseTokensNullTokenListTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                IEnumerable<TokenResponse> _ = sut.ParseTokens("cacheRegion", "response", null).ToArray();
            });
        }

        [TestMethod]
        public void ParseTokensEmptyTokenListTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", "response", new TokenInfo[0]);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void ParseTokensNullXpathNodeTest()
        {
            var xmlDocument = "<test></test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = null
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.IsNull(token.Value);
        }

        [TestMethod]
        public void ParseTokensGoodXpathNodeTest()
        {
            var xmlDocument = "<test></test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = "/"
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(xmlDocument, token.Value);
        }

        [TestMethod]
        public void ParseTokensGoodXpathTextTest()
        {
            var expectedText = "Text";
            var xmlDocument = $"<test>{expectedText}</test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = "/test/text()"
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expectedText, token.Value);
        }

        [TestMethod]
        public void ParseTokensBadXpathTextTest()
        {
            var expectedText = "Text";
            var xmlDocument = $"<test><node>{expectedText}</node></test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = "/test1/text()"
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.NotFound, token.Status);
            Assert.IsNull(token.Value);
        }

        [TestMethod]
        public void ParseTokensBadXpathTest()
        {
            var expectedText = "Text";
            var xmlDocument = $"<test>{expectedText}</test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = "/test/tex()"
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Error, token.Status);
            Assert.IsNull(token.Value);
        }

        [TestMethod]
        public void ParseTokensNamespaceRemovalTest()
        {
            var expectedText = "Text";
            var xmlDocument = $"<test xmlns=\"https://example.com/\" xmlns:a=\"https://example.com/\">{expectedText}</test>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        XPath = "/test/text()"
                    }
                }
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expectedText, token.Value);
        }

        [TestMethod]
        public void ParseTokensCachedTokenTest()
        {
            Guid tokenId = Guid.NewGuid();
            var expectedText = "Text";
            var updatedText = "Update Text";
            var xmlDocument = $"<test>{updatedText}</test>";
            var cacheRegion = "cacheRegion";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XPathTokenInfo
                    {
                        Id = tokenId,
                        Name = "Test token",
                        XPath = "/test/text()"
                    },
                    new XPathTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token 2",
                        XPath = "/test/text()"
                    }
                }
            };

            var cache = Substitute.For<ICache>();
            cache.Get(cacheRegion, tokenId.ToString()).Returns(new CacheEntry { Value = expectedText });

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens(cacheRegion, xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expectedText, token.Value);
        }

        #endregion

        #region ParseRelativeToken

        [TestMethod]
        public void ParseRelativeTokenNullCacheRegion()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                TokenResponse _ = sut.ParseRelativeToken(null, null, null, 0);
            });
        }

        [TestMethod]
        public void ParseRelativeTokenNullBaseToken()
        {
            var cacheRegion = "cacheRegion";

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                TokenResponse _ = sut.ParseRelativeToken(cacheRegion, null, null, 0);
            });
        }

        [TestMethod]
        public void ParseRelativeTokenNullRelativeToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                TokenResponse _ = sut.ParseRelativeToken(cacheRegion, baseToken, null, 0);
            });
        }

        [TestMethod]
        public void ParseRelativeTokenInvalidIndex()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new TokenInfo
            {
                Id = relativeTokenId
            };

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                TokenResponse _ = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, 0);
            });
        }

        [TestMethod]
        public void ParseRelativeTokenUncachedToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new TokenInfo
            {
                Id = relativeTokenId
            };
            var index = 1;

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenInvalidBaseTokenXml()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new TokenInfo
            {
                Id = relativeTokenId
            };
            var index = 1;
            var cacheEntry = new CacheEntry
            {
                Value = "<a>b"
            };

            var cache = Substitute.For<ICache>();

            cache.Get(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.Error, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenToHighIndex()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new TokenInfo
            {
                Id = relativeTokenId
            };
            var index = 20;
            var cacheEntry = new CacheEntry
            {
                Value = "<node>a</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenValidToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var tokenContent = "a";
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new XPathTokenInfo
            {
                Id = relativeTokenId,
                XPath = "/node/text()"
            };
            var index = 1;
            var cacheEntry = new CacheEntry
            {
                Value = $"<node>{tokenContent}</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse.Status);
            Assert.IsNotNull(tokenResponse.Value);
            Assert.AreEqual(tokenContent, tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenNotFoundToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var tokenContent = "a";
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new XPathTokenInfo
            {
                Id = relativeTokenId,
                XPath = "/node1/text()"
            };
            var index = 1;
            var cacheEntry = new CacheEntry
            {
                Value = $"<node>{tokenContent}</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenEmptyBaseToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new XPathTokenInfo
            {
                Id = relativeTokenId,
                XPath = "/node1/text()"
            };
            var index = 1;
            var cacheEntry = new CacheEntry
            {
                Value = ""
            };

            var cache = Substitute.For<ICache>();

            cache.Get(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.Id);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        #endregion
    }
}