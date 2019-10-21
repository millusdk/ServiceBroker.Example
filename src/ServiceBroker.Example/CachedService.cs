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
    public class CachedService : ServiceBase, ICachedService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly ITokenService _tokenService;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CachedService(ICache cache, IHttpClientWrapper httpClientWrapper, ITokenService tokenService) : base(cache)
        {
            _httpClientWrapper = httpClientWrapper;
            _tokenService = tokenService;
        }

        protected override async Task<ServiceResponse> CallServiceInternal(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken,
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
                        Id = serviceInfo.Id,
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
                        Id = serviceInfo.Id,
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
                        GetPostParameters(cacheRegion, serviceInfo, additionalParameters);

                    HttpClientResponse response =
                        await _httpClientWrapper.PostAsync(serviceInfo.Endpoint, postParameters, cancelToken);
                    var serviceResponse = new ServiceResponse
                    {
                        Id = serviceInfo.Id
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
                    Id = serviceInfo.Id,
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