using System.Collections.Generic;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// This interface represents a service that is able to evaluate one or more tokens on a text input or on the stored value of another token.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Evaluates the specified tokens on the service response.
        /// If the value of a token is available in the cache, it is retrieved from there.
        /// </summary>
        /// <param name="cacheRegion">The cache region to look for token values under.</param>
        /// <param name="serviceResponse">The response from the service to evaluate the tokens on.</param>
        /// <param name="tokens">The tokens to evaluate on the service response.</param>
        /// <returns>A list of the results </returns>
        IEnumerable<TokenResponse> ParseTokens(string cacheRegion, string serviceResponse, IEnumerable<TokenInfo> tokens);

        /// <summary>
        /// Evaluates a token on the partial cached value of another token.
        /// </summary>
        /// <param name="cacheRegion">The cache region to read the outer token value from.</param>
        /// <param name="baseToken">The outer token whose content the inner token should be evaluated on.</param>
        /// <param name="relativeToken">The inner token to evaluate on the partial content of the outer token.</param>
        /// <param name="index">The index for the element in the outer token to apply the inner token to.</param>
        /// <returns>A token response containing information about whether the evaluation of the token succeeded.</returns>
        TokenResponse ParseRelativeToken(string cacheRegion, TokenInfo baseToken, TokenInfo relativeToken, int index);
    }
}