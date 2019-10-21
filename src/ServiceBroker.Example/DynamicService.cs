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
    public class DynamicService : ServiceBase, IDynamicService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly ITokenService _tokenService;

        public DynamicService(ICache cache, IHttpClientWrapper httpClientWrapper, ITokenService tokenService) : base(cache)
        {
            _httpClientWrapper = httpClientWrapper;
            _tokenService = tokenService;
        }

        protected override async Task<ServiceResponse> CallServiceInternal(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            var serviceResponse = new ServiceResponse
            {
                Id = serviceInfo.Id
            };

            try
            {
                AsyncPolicyWrap<ServiceResponse> breaker = GetCircuitBreakerPolicy(serviceInfo);

                return await breaker.ExecuteAsync(async (cancelToken) =>
                {
                    IEnumerable<KeyValuePair<string, string>> postParameters = GetPostParameters(cacheRegion, serviceInfo, additionalParameters);

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