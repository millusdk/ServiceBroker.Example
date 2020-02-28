using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    /// <summary>
    /// A service that always returns the same answer no matter which user the service is called for
    /// </summary>
    public class StaticService : ServiceBase, IStaticService
    {
        private readonly ICache _cache;
        private readonly ITokenService _tokenService;

        public StaticService(ICache cache, ITokenService tokenService)
        {
            _cache = cache;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Returns the configured data for the service
        /// </summary>
        /// <param name="serviceInfo">The service to be called</param>
        /// <param name="cacheRegion">The cache region to store/retrieve values from if applicable</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the request to the service.</param>
        /// <param name="additionalParameters">Additional parameters to include in the request if they have a value available in the cache.</param>
        /// <returns></returns>
        public override Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            if (!(serviceInfo is StaticServiceInfo staticServiceInfo))
            {
                return Task.FromResult<ServiceResponse>(null);
            }

            if (string.IsNullOrEmpty(staticServiceInfo.Data))
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