using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    public class StaticService : ServiceBase, IStaticService
    {
        private readonly ICache _cache;
        private readonly ITokenService _tokenService;

        public StaticService(ICache cache, ITokenService tokenService)
        {
            _cache = cache;
            _tokenService = tokenService;
        }

        public override Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            if (!(serviceInfo is StaticServiceInfo staticServiceInfo))
            {
                return Task.FromResult<ServiceResponse>(null);
            }

            if (string.IsNullOrEmpty(staticServiceInfo?.Data))
            {
                return Task.FromResult<ServiceResponse>(null);
            }

            var serviceResponse = new ServiceResponse
            {
                ServiceId = staticServiceInfo.Id,
                Status = ServiceResponseStatus.Success,
            };

            serviceResponse.Value = staticServiceInfo.Data;
            serviceResponse.TokenResponses = _tokenService.ParseTokens(cacheRegion, staticServiceInfo.Data, serviceInfo.Tokens);

            foreach (TokenResponse token in serviceResponse.TokenResponses)
            {
                _cache.Set(cacheRegion, token.CacheKey, token.Value);
            }

            return Task.FromResult(serviceResponse);
        }
    }
}