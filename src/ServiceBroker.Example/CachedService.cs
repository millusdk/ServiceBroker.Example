using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly.Wrap;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Common.Extensions;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    /// <summary>
    /// A called service wrapper which saves the values returned from the called service in the cache, such that consecutive calls within the same cache region reads data from the cache rather than call the called service.
    /// </summary>
    public class CachedService : CalledServiceBase, ICachedService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly ITokenService _tokenService;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CachedService(ICache cache, IHttpClientWrapper httpClientWrapper, ITokenService tokenService) : base(cache)
        {
            _httpClientWrapper = httpClientWrapper;
            _tokenService = tokenService;
        }

        /// <summary>
        /// This method performs the call to the cached service if no value is available in the cache.
        /// </summary>
        /// <param name="serviceInfo">Information about the service to call</param>
        /// <param name="cacheRegion">The cache region to look for an existing response and to look for values for post parameters under in</param>
        /// <param name="cancellationToken">Cancellation token to cancel the request</param>
        /// <param name="additionalParameters">Additional post parameters to include in the request body</param>
        /// <returns>A service response representing the result of the call to the cached service</returns>
        protected override async Task<ServiceResponse> CallServiceInternal(CalledServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            SemaphoreSlim semaphore = _semaphores.GetOrAdd($"{cacheRegion}-{serviceInfo.CacheKey}", _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return new ServiceResponse
                    {
                        ServiceId = serviceInfo.Id,
                        Status = ServiceResponseStatus.Timeout
                    };
                }

                CacheEntry<string> cacheResult = Cache.Get<string>(cacheRegion, serviceInfo.CacheKey);

                if (cacheResult != null)
                {
                    Log.Debug("Read value for service {ServiceName} from cache. Entry has value: {HasValue}",
                        serviceInfo.Name, cacheResult.Value != null);

                    ServiceResponse serviceResponse = new ServiceResponse
                    {
                        ServiceId = serviceInfo.Id,
                        Status = ServiceResponseStatus.Success,
                        Value = cacheResult.Value
                    };

                    if (string.IsNullOrEmpty(serviceResponse.Value))
                    {
                        return serviceResponse;
                    }

                    serviceResponse.TokenResponses =
                        _tokenService.ParseTokens(cacheRegion, cacheResult.Value, serviceInfo.Tokens);

                    foreach (TokenResponse token in serviceResponse.TokenResponses)
                    {
                        Cache.Set(cacheRegion, token.CacheKey, token.Value);
                    }

                    return serviceResponse;
                }

                AsyncPolicyWrap<ServiceResponse> breaker = GetCircuitBreakerPolicy(serviceInfo);

                return await breaker.ExecuteAsync(async (cancelToken) =>
                {
                    IEnumerable<KeyValuePair<string, string>> postParameters =
                        GetPostParameters(serviceInfo, cacheRegion, additionalParameters);

                    HttpClientResponse response =
                        await _httpClientWrapper.PostAsync(serviceInfo.Endpoint, postParameters, cancelToken);
                    var serviceResponse = new ServiceResponse
                    {
                        ServiceId = serviceInfo.Id
                    };

                    if (response.HttpStatusCode.IsOkStatus())
                    {
                        serviceResponse.Value = response.Response;
                        serviceResponse.Status = ServiceResponseStatus.Success;
                        serviceResponse.TokenResponses =
                            _tokenService.ParseTokens(cacheRegion, response.Response, serviceInfo.Tokens);

                        foreach (TokenResponse token in serviceResponse.TokenResponses)
                        {
                            Cache.Set(cacheRegion, token.CacheKey, token.Value);
                        }

                        Cache.Set(cacheRegion, serviceInfo.CacheKey, response.Response);
                    }
                    else
                    {
                        Cache.Set<string>(cacheRegion, serviceInfo.CacheKey, null);
                        serviceResponse.Status = ServiceResponseStatus.Error;
                    }

                    return serviceResponse;
                }, cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return new ServiceResponse
                {
                    ServiceId = serviceInfo.Id,
                    Status = ServiceResponseStatus.Timeout
                };
            }
            finally
            {
                if (semaphore.CurrentCount == 0)
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Removes all cached semaphores (lock objects) stored under the specified cache region.
        /// </summary>
        /// <param name="cacheRegion">The cache region to remove semaphores for</param>
        /// <returns>A bool signifying whether all semaphores where removed successfully</returns>
        [ExcludeFromCodeCoverage]
        public bool RemoveSemaphores(string cacheRegion)
        {
            var retVal = true;
            IEnumerable<string> keys = _semaphores.Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.StartsWith(cacheRegion))
                {
                    if (!_semaphores.TryRemove(key, out _))
                    {
                        retVal = false;
                    }
                }
            }

            return retVal;
        }
    }
}