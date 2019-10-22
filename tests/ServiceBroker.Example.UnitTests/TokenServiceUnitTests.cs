using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceBroker.Example.Mocks;

namespace ServiceBroker.Example.UnitTests
{
    [TestClass]
    public class TokenServiceUnitTests
    {

        #region ParseTokensXpath

        [TestMethod]
        public void ParseTokensXpathEmptyServiceResponseTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                IEnumerable<TokenResponse> _ = sut.ParseTokens("cacheRegion", "", null).ToArray();
            });
        }

        [TestMethod]
        public void ParseTokensXpathNullTokenListTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                IEnumerable<TokenResponse> _ = sut.ParseTokens("cacheRegion", "response", null).ToArray();
            });
        }

        [TestMethod]
        public void ParseTokensXpathEmptyTokenListTest()
        {
            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", "response", new TokenInfo[0]);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public void ParseTokensXpathNullXpathNodeTest()
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
        public void ParseTokensXpathGoodXpathNodeTest()
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
        public void ParseTokensXpathGoodXpathTextTest()
        {
            var expectedText = "Text";
            string xmlDocument = $"<test>{expectedText}</test>";
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
        public void ParseTokensXpathBadXpathTextTest()
        {
            var expectedText = "Text";
            string xmlDocument = $"<test><node>{expectedText}</node></test>";
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
        public void ParseTokensXpathBadXpathTest()
        {
            var expectedText = "Text";
            string xmlDocument = $"<test>{expectedText}</test>";
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
        public void ParseTokensXpathNamespaceRemovalTest()
        {
            var expectedText = "Text";
            string xmlDocument = $"<test xmlns=\"https://example.com/\" xmlns:a=\"https://example.com/\">{expectedText}</test>";
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
        public void ParseTokensXpathCachedTokenTest()
        {
            Guid tokenId = Guid.NewGuid();
            var expectedText = "Text";
            var updatedText = "Update Text";
            string xmlDocument = $"<test>{updatedText}</test>";
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
            cache.Get<string>(cacheRegion, tokenId.ToString()).Returns(new CacheEntry<string> { Value = expectedText });

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens(cacheRegion, xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expectedText, token.Value);
        }

        #endregion

        #region ParseTokensXslt

        [TestMethod]
        public void ParseTokensXsltGoodXsltTest()
        {
            var expectedText = "Hello, World!";
            string xmlDocument = $"<?xml version=\"1.0\"?><hello-world><greeting>{expectedText}</greeting></hello-world>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XsltTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        Xslt = "<xsl:template match=\"/hello-world\"><xsl:value-of select=\"greeting\"/></xsl:template>"
                    }
                }
            };
            string expected = $"{expectedText}";

            var cache = Substitute.For<ICache>();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens);

            IEnumerable<TokenResponse> tokenResponses = actual as TokenResponse[] ?? actual.ToArray();
            Assert.IsTrue(tokenResponses.Any());

            TokenResponse token = tokenResponses.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expected, token.Value);
        }

        [TestMethod]
        public void ParseTokensXsltGoodXsltWithCacheTest()
        {
            var expectedText = "Hello, World!";
            string xmlDocument = $"<?xml version=\"1.0\"?><hello-world><greeting>{expectedText}</greeting></hello-world>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XsltTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        Xslt = "<xsl:template match=\"/hello-world\"><xsl:value-of select=\"greeting\"/></xsl:template>"
                    }
                }
            };
            string expected = $"{expectedText}";

            var cache = new CacheMock();

            var sut = new TokenService(cache);

            IEnumerable<TokenResponse> _ = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens).ToArray();
            IEnumerable<TokenResponse> actual = sut.ParseTokens("cacheRegion", xmlDocument, serviceInfo.Tokens).ToArray();

            Assert.IsTrue(actual.Any());

            TokenResponse token = actual.First();

            Assert.AreEqual(TokenResponseStatus.Found, token.Status);
            Assert.AreEqual(expected, token.Value);
        }

        [TestMethod]
        public void ParseTokensXsltBadXsltTest()
        {
            var expectedText = "Hello, World!";
            string xmlDocument = $"<?xml version=\"1.0\"?><hello-world><greeting>{expectedText}</greeting></hello-world>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XsltTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        Xslt = "<xsl:template match=\"/hello-world\"><xsl:value-of select=\"greeting\"/>"
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
        public void ParseTokensXsltNoMatchTest()
        {
            string xmlDocument = $"<?xml version=\"1.0\"?><hello-world><greeting>Hello, World!</greeting></hello-world>";
            var serviceInfo = new ServiceInfo()
            {
                Tokens = new[]
                {
                    new XsltTokenInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test token",
                        Xslt = "<xsl:template match=\"/test\"><xsl:value-of select=\"test2\"/></xsl:template>"
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
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
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
            var cacheEntry = new CacheEntry<string>
            {
                Value = "<a>b"
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
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
            var cacheEntry = new CacheEntry<string>
            {
                Value = "<node>a</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenValidXPathToken()
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
            var cacheEntry = new CacheEntry<string>
            {
                Value = $"<node>{tokenContent}</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse.Status);
            Assert.IsNotNull(tokenResponse.Value);
            Assert.AreEqual(tokenContent, tokenResponse.Value);
        }

        [TestMethod]
        public void ParseRelativeTokenValidXsltToken()
        {
            var cacheRegion = "cacheRegion";
            Guid baseTokenId = Guid.NewGuid();
            Guid relativeTokenId = Guid.NewGuid();
            var expectedText = "Hello, World!";
            var baseToken = new TokenInfo
            {
                Id = baseTokenId
            };
            var relativeToken = new XsltTokenInfo
            {
                Id = relativeTokenId,
                Name = "Test token",
                Xslt = "<xsl:template match=\"/hello-world\"><xsl:value-of select=\"greeting\"/></xsl:template>"
            };
            var index = 1;
            var cacheEntry = new CacheEntry<string>
            {
                Value = $"<hello-world><greeting>{expectedText}</greeting></hello-world><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
            Assert.AreEqual(TokenResponseStatus.Found, tokenResponse.Status);
            Assert.IsNotNull(tokenResponse.Value);
            Assert.AreEqual(expectedText, tokenResponse.Value);
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
            var cacheEntry = new CacheEntry<string>
            {
                Value = $"<node>{tokenContent}</node><node>b</node>"
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
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
            var cacheEntry = new CacheEntry<string>
            {
                Value = ""
            };

            var cache = Substitute.For<ICache>();

            cache.Get<string>(cacheRegion, baseTokenId.ToString()).Returns(cacheEntry);

            var sut = new TokenService(cache);

            TokenResponse tokenResponse = sut.ParseRelativeToken(cacheRegion, baseToken, relativeToken, index);

            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(relativeTokenId, tokenResponse.TokenId);
            Assert.AreEqual(TokenResponseStatus.NotFound, tokenResponse.Status);
            Assert.IsNull(tokenResponse.Value);
        }

        #endregion
    }
}