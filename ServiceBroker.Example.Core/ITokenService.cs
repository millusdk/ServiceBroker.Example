using System.Collections.Generic;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface ITokenService
    {
        IEnumerable<TokenResponse> ParseTokens(string cacheRegion, string serviceResponse, IEnumerable<TokenInfo> tokens);

        TokenResponse ParseRelativeToken(string cacheRegion, TokenInfo baseToken, TokenInfo relativeToken, int index);
    }
}