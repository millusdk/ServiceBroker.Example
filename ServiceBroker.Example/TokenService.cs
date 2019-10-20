using Serilog;
using Serilog.Core;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ServiceBroker.Example
{
    public class TokenService : ITokenService
    {
        private readonly ICache _cache;

        [ExcludeFromCodeCoverage]
        // ReSharper disable once MemberCanBePrivate.Global
        public ILogger Log { get; set; } = Logger.None;

        public TokenService(ICache cache)
        {
            _cache = cache;
        }

        public IEnumerable<TokenResponse> ParseTokens(string cacheRegion, string serviceResponse, IEnumerable<TokenInfo> tokens)
        {
            if (cacheRegion == null)
            {
                throw new ArgumentNullException(nameof(cacheRegion));
            }

            if (serviceResponse == null)
            {
                throw new ArgumentNullException(nameof(serviceResponse));
            }

            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            IEnumerable<TokenInfo> tokenInfos = tokens as TokenInfo[] ?? tokens.ToArray();
            if (!tokenInfos.Any())
            {
                yield break;
            }

            foreach(TokenInfo token in tokenInfos)
            {
                TokenResponse cachedToken = GetTokenFromCache(cacheRegion, token);

                if (cachedToken != null)
                {
                    yield return cachedToken;
                }
                else
                {
                    if(token is XPathTokenInfo xPathToken)
                    {
                        var tokenResponse = new TokenResponse
                        {
                            Id = token.Id
                        };

                        try
                        {
                            var tokenValue = ProcessXpath(serviceResponse, xPathToken.XPath);
                            tokenResponse.Status = string.IsNullOrEmpty(tokenValue) ? TokenResponseStatus.NotFound : TokenResponseStatus.Found;
                            tokenResponse.Value = tokenValue;
                        }
                        catch(Exception)
                        {
                            Log.Warning("Failed to execute token {TokenId}", token.Id);
                            tokenResponse.Status = TokenResponseStatus.Error;
                        }

                        yield return tokenResponse;
                    }
                }

            }
        }

        public TokenResponse ParseRelativeToken(string cacheRegion, TokenInfo baseToken, TokenInfo relativeToken, int index)
        {
            if (cacheRegion == null)
            {
                throw new ArgumentNullException(nameof(cacheRegion));
            }

            if (baseToken == null)
            {
                throw new ArgumentNullException(nameof(baseToken));
            }

            if (relativeToken == null)
            {
                throw new ArgumentNullException(nameof(relativeToken));
            }

            if (index < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Parameter \"{nameof(index)}\" must be a positive integer.");
            }

            TokenResponse cachedBaseToken = GetTokenFromCache(cacheRegion, baseToken);

            if (cachedBaseToken == null)
            {
                return new TokenResponse
                {
                    Id = relativeToken.Id,
                    Status = TokenResponseStatus.NotFound
                };
            }

            try
            {
                XElement xElement = XElement.Parse($"<root>{cachedBaseToken.Value}</root>");

                if (!xElement.HasElements)
                {
                    return new TokenResponse
                    {
                        Id = relativeToken.Id,
                        Status = TokenResponseStatus.NotFound
                    };
                }

                XElement[] children = xElement.Elements().ToArray();

                if (children.Length < index)
                {
                    return new TokenResponse
                    {
                        Id = relativeToken.Id,
                        Status = TokenResponseStatus.NotFound
                    };
                }

                XElement targetChild = children[index - 1];

                IEnumerable<TokenResponse> tokenValues = ParseTokens(cacheRegion, targetChild.ToString(), new[] {relativeToken});

                TokenResponse tokenValue = tokenValues.FirstOrDefault();

                return tokenValue;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured when parsing relative token ({RelativeToken}) against base token ({BaseToken}) with index ({Index})", relativeToken.Id, baseToken.Id, index);
                return new TokenResponse
                {
                    Id = relativeToken.Id,
                    Status = TokenResponseStatus.Error
                };
            }
        }

        private TokenResponse GetTokenFromCache(string cacheRegion, TokenInfo tokenInfo)
        {
            CacheEntry cacheEntry = _cache.Get(cacheRegion, tokenInfo.CacheKey);

            if (cacheEntry != null)
            {
                return new TokenResponse
                {
                    Id = tokenInfo.Id,
                    Status = TokenResponseStatus.Found,
                    Value = cacheEntry.Value
                };
            }

            return null;
        }

        #region Process Xpath

        private static string ProcessXpath(string result, string xPath)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(result));
            var t = xmlDocumentWithoutNs.ToString();

            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            //xmlDocument.LoadXml(result);
            xmlDocument.LoadXml(t);
            XmlElement document = xmlDocument.DocumentElement;
            if (xPath == null)
            {
                return null;
            }

            // use single xpath extraction                
            // ReSharper disable once PossibleNullReferenceException
            // document variable cannot be null, as this results in an exception
            // from xmlDocument.LoadXml();
            XmlNode xPathNode = document.SelectSingleNode(xPath);
            return xPathNode?.NodeType == XmlNodeType.Text ? xPathNode.Value : xPathNode?.InnerXml;
        }

        private static XElement RemoveAllNamespaces(XElement element)
        {
            object content;
            if(element.HasElements)
            {
                content = element.Elements().Select(RemoveAllNamespaces);
            }
            else
            {
                content = element.Value;
            }

            XElement xElement = new XElement(element.Name.LocalName, content);

            xElement.ReplaceAttributes(element.Attributes().Where(attr => (!attr.IsNamespaceDeclaration)));

            return xElement;
        }

        #endregion
    }
}
