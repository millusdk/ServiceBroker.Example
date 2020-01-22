using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Common.Extensions;
using ServiceBroker.Example.Models;
using System.Linq;
using Polly.Wrap;

namespace ServiceBroker.Example
{
    /// <summary>
    /// A called service wrapper, where the called service should be called every time wrapper is called.
    /// </summary>
    public class DynamicService : CalledServiceBase, IDynamicService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly ITokenService _tokenService;

        public DynamicService(ICache cache, IHttpClientWrapper httpClientWrapper, ITokenService tokenService) : base(cache)
        {
            _httpClientWrapper = httpClientWrapper;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Calls the dynamic service and returns the result.
        /// </summary>
        /// <param name="serviceInfo">Information about the service to call</param>
        /// <param name="cacheRegion">The cache region to look for values for post parameters under in</param>
        /// <param name="cancellationToken">Cancellation token to cancel the request</param>
        /// <param name="additionalParameters">Additional post parameters to include in the request body</param>
        /// <returns>A service response representing the result of the call to the dynamic service</returns>
        protected override async Task<ServiceResponse> CallServiceInternal(CalledServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            var serviceResponse = new ServiceResponse
            {
                ServiceId = serviceInfo.Id
            };

            try
            {
                AsyncPolicyWrap<ServiceResponse> breaker = GetCircuitBreakerPolicy(serviceInfo);

                return await breaker.ExecuteAsync(async (cancelToken) =>
                {
                    IEnumerable<KeyValuePair<string, string>> postParameters = GetPostParameters(serviceInfo, cacheRegion, additionalParameters);

                    HttpClientResponse response = await _httpClientWrapper.PostAsync(serviceInfo.Endpoint, postParameters, cancelToken);


                    if (response.HttpStatusCode.IsOkStatus())
                    {
                        serviceResponse.Value = response.Response;
                        serviceResponse.Status = ServiceResponseStatus.Success;
                        serviceResponse.TokenResponses = _tokenService.ParseTokens(cacheRegion, response.Response, serviceInfo.Tokens).ToArray();
                    }
                    else
                    {
                        serviceResponse.Status = ServiceResponseStatus.Error;
                    }

                    return serviceResponse;
                }, cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                serviceResponse.Status = ServiceResponseStatus.Error;
                return serviceResponse;
            }
        }
    }
}