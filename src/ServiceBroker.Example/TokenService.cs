﻿using Serilog;
using Serilog.Core;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Saxon.Api;

namespace ServiceBroker.Example
{
    /// <summary>
    /// A service handling parsing and evaluating tokens.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly ICache _cache;
        // ReSharper disable ConvertToConstant.Local
        private static readonly string NoMatchString = "-------NO MATCH-------";
        private static readonly string XsltCacheRegion = "XSLT";
        // ReSharper enable ConvertToConstant.Local

        /// <summary>
        /// Log used to write information about the result of evaluating tokens. Defaults to a null logger.
        /// </summary>
        [ExcludeFromCodeCoverage]
        // ReSharper disable once MemberCanBePrivate.Global
        public ILogger Log { get; set; } = Logger.None;

        public TokenService(ICache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Evaluates the specified tokens on the service response.
        /// If the value of a token is available in the cache, it is retrieved from there.
        /// </summary>
        /// <param name="cacheRegion">The cache region to look for token values under.</param>
        /// <param name="serviceResponse">The response from the service to evaluate the tokens on.</param>
        /// <param name="tokens">The tokens to evaluate on the service response.</param>
        /// <returns>A list of the results </returns>
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
                    var tokenResponse = new TokenResponse
                    {
                        TokenId = token.Id
                    };

                    switch (token)
                    {
                        case XPathTokenInfo xPathToken:
                            try
                            {
                                string tokenValue = ProcessXpath(serviceResponse, xPathToken.XPath);
                                tokenResponse.Status = string.IsNullOrEmpty(tokenValue) ? TokenResponseStatus.NotFound : TokenResponseStatus.Found;
                                tokenResponse.Value = tokenValue;
                            }
                            catch(Exception ex)
                            {
                                Log.Error(ex,"Failed to execute token {TokenId}", token.Id);
                                tokenResponse.Status = TokenResponseStatus.Error;
                            }

                            break;
                        case XsltTokenInfo xsltToken:
                            try
                            {
                                string tokenValue = ProcessXslt(serviceResponse, xsltToken.Xslt);
                                tokenResponse.Status = string.IsNullOrEmpty(tokenValue)
                                    ? TokenResponseStatus.NotFound
                                    : TokenResponseStatus.Found;
                                tokenResponse.Value = tokenValue;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to execute token {TokenId}", token.Id);
                                tokenResponse.Status = TokenResponseStatus.Error;
                            }

                            break;
                    }

                    yield return tokenResponse;
                }

            }
        }

        /// <summary>
        /// Evaluates a token on the partial cached value of another token.
        /// </summary>
        /// <param name="cacheRegion">The cache region to read the outer token value from.</param>
        /// <param name="baseToken">The outer token whose content the inner token should be evaluated on.</param>
        /// <param name="relativeToken">The inner token to evaluate on the partial content of the outer token.</param>
        /// <param name="index">The index for the element in the outer token to apply the inner token to.</param>
        /// <returns>A token response containing information about whether the evaluation of the token succeeded.</returns>
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
                    TokenId = relativeToken.Id,
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
                        TokenId = relativeToken.Id,
                        Status = TokenResponseStatus.NotFound
                    };
                }

                XElement[] children = xElement.Elements().ToArray();

                if (children.Length < index)
                {
                    return new TokenResponse
                    {
                        TokenId = relativeToken.Id,
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
                    TokenId = relativeToken.Id,
                    Status = TokenResponseStatus.Error
                };
            }
        }

        private TokenResponse GetTokenFromCache(string cacheRegion, TokenInfo tokenInfo)
        {
            CacheEntry<string> cacheEntry = _cache.Get<string>(cacheRegion, tokenInfo.CacheKey);

            if (cacheEntry != null)
            {
                return new TokenResponse
                {
                    TokenId = tokenInfo.Id,
                    Status = TokenResponseStatus.Found,
                    Value = cacheEntry.Value
                };
            }

            return null;
        }

        private static XElement RemoveAllNamespaces(XElement element)
        {
            object content;
            if (element.HasElements)
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

        #region Process Xpath

        private static string ProcessXpath(string result, string xPath)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(result));
            string t = xmlDocumentWithoutNs.ToString();

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

        #endregion

        #region Process Xslt

        private string ProcessXslt(string result, string xslt)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(result));
            string cleanXmlDocument = xmlDocumentWithoutNs.ToString();

            XsltExecutable xsltExecutable = GetXsltExecutable(xslt);

            byte[] xmlByteArray = System.Text.Encoding.UTF8.GetBytes(cleanXmlDocument);
            var inputStream = new MemoryStream(xmlByteArray);
            XsltTransformer transformer = xsltExecutable.Load();
            // Saxon requires to set an Uri for the stream; otherwise setting the input stream fails
            transformer.SetInputStream(inputStream, new Uri("http://localhost"));

            // run the transformation and save the result to string
            Serializer serializer = new Processor().NewSerializer();
            using (var memoryStream = new MemoryStream())
            {
                serializer.SetOutputStream(memoryStream);
                transformer.Run(serializer);
                memoryStream.Position = 0;
                using (var streamReader = new StreamReader(memoryStream))
                {
                    result = streamReader.ReadToEnd();
                }
            }

            return result.Contains(NoMatchString) ? null : result;
        }

        private XsltExecutable GetXsltExecutable(string xslt)
        {
            var cacheEntry = _cache.Get<XsltExecutable>(XsltCacheRegion, xslt);

            if (cacheEntry?.Value != null)
            {
                return cacheEntry.Value;
            }

            var processor = new Processor();
            XsltCompiler compiler = processor.NewXsltCompiler();
            // create stream from input xslt
            string fullXslt =
                "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
                "<xsl:output method=\"text\" />" +
                $"{xslt}" +
                $"<xsl:template match=\"/*\" priority=\"0\">{NoMatchString}</xsl:template></xsl:stylesheet>";
            byte[] xsltByteArray = System.Text.Encoding.UTF8.GetBytes(fullXslt);
            var xsltStream = new MemoryStream(xsltByteArray);
            XsltExecutable xsltExecutable = compiler.Compile(xsltStream);

            _cache.Set(XsltCacheRegion, xslt, xsltExecutable);
            return xsltExecutable;
        }

        #endregion
    }
}
