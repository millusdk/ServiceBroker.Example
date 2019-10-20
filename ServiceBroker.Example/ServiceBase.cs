using Polly;
using Polly.Timeout;
using Polly.Wrap;
using Serilog;
using Serilog.Core;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Fallback;

namespace ServiceBroker.Example
{
    public abstract class ServiceBase : IServiceBase
    {
        private static readonly ConcurrentDictionary<string, AsyncPolicyWrap<ServiceResponse>> CircuitBreakerPolicies =
            new ConcurrentDictionary<string, AsyncPolicyWrap<ServiceResponse>>();

        protected readonly ICache Cache;

        [ExcludeFromCodeCoverage]
        public ILogger Log { get; set; } = Logger.None;

        protected ServiceBase(ICache cache)
        {
            Cache = cache;
        }

        public IEnumerable<KeyValuePair<string, string>> GetPostParameters(string cacheRegion,
            ServiceInfo serviceInfo, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            List<KeyValuePair<string, string>> pairs = additionalParameters?.ToList() ?? new List<KeyValuePair<string, string>>();

            CacheEntry userIdentifier = Cache.Get(cacheRegion, Common.Constants.UserIdentifierCacheKey);

            if (userIdentifier != null)
            {
                pairs.Add(new KeyValuePair<string, string>(Common.Constants.UserIdentifierPostKey, userIdentifier.Value));
            }

            if (serviceInfo.AdditionalParameters == null)
            {
                return pairs;
            }

            if (!serviceInfo.AdditionalParameters.Any())
            {
                return pairs;
            }

            List<KeyValuePair<string, string>> additionalUserParameters = serviceInfo.AdditionalParameters
                .Select(additionalParameter => new
                {
                    additionalParameter,
                    parameterValue = Cache.Get(cacheRegion, additionalParameter.Id.ToString())?.Value
                })
                .Where(additionalParameter => !string.IsNullOrEmpty(additionalParameter.parameterValue))
                .Select(additionalParameter => new KeyValuePair<string, string>(additionalParameter.additionalParameter.Name, additionalParameter.parameterValue))
                .ToList();
            pairs.AddRange(
                additionalUserParameters
                );

            return pairs;
        }

        protected AsyncPolicyWrap<ServiceResponse> GetCircuitBreakerPolicy(ServiceInfo serviceInfo)
        {
            return CircuitBreakerPolicies.GetOrAdd(serviceInfo.CacheKey, _ =>
            {
                AsyncTimeoutPolicy timeoutPolicy = Policy
                    .TimeoutAsync(
                        serviceInfo.Timeout,
                        TimeoutStrategy.Pessimistic
                    );

                AsyncCircuitBreakerPolicy<ServiceResponse> circuitBreakerPolicy = Policy<ServiceResponse>
                    .Handle<TimeoutRejectedException>()
                    .OrResult(resultPredicate: serviceResponse => serviceResponse.Status == ServiceResponseStatus.Error)
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: serviceInfo.ExceptionCount,
                        durationOfBreak: serviceInfo.BreakDuration,
                        onBreak: (__, ___) =>
                        {
                            Log.Warning("Service ({ServiceName}) has reached its threshold for the circuit breaker and the circuit has been opened", serviceInfo.Name);
                        },
                        onReset: () =>
                        {
                            Log.Warning("Service ({ServiceName}) has been determined to be back up, circuit closed again", serviceInfo.Name);
                        }
                    );

                AsyncPolicyWrap<ServiceResponse> circuitBreakerWrappingTimeout = circuitBreakerPolicy
                    .WrapAsync(timeoutPolicy);

                AsyncFallbackPolicy<ServiceResponse> timeoutFallbackPolicy = Policy<ServiceResponse>
                    .Handle<TimeoutRejectedException>()
                    .FallbackAsync(
                        cancellationToken =>
                        {
                            return Task.FromResult(new ServiceResponse
                            {
                                Status = ServiceResponseStatus.Timeout,
                                Id = serviceInfo.Id
                            });
                        });
                AsyncPolicyWrap<ServiceResponse> timeoutFallbackPolicyWrappingCircuitBreaker = timeoutFallbackPolicy
                    .WrapAsync(circuitBreakerWrappingTimeout);

                AsyncFallbackPolicy<ServiceResponse> exceptionFallbackPolicy = Policy<ServiceResponse>
                    .Handle<Exception>()
                    .FallbackAsync(
                        cancellationToken =>
                        {
                            return Task.FromResult(new ServiceResponse
                            {
                                Status = ServiceResponseStatus.Error,
                                Id = serviceInfo.Id
                            });
                        });
                AsyncPolicyWrap<ServiceResponse> exceptionFallbackPolicyWrappingTimeoutFallback = exceptionFallbackPolicy
                    .WrapAsync(timeoutFallbackPolicyWrappingCircuitBreaker);

                AsyncPolicyWrap<ServiceResponse> policy = exceptionFallbackPolicyWrappingTimeoutFallback;

                return policy;
            });
        }

        public async Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            if (string.IsNullOrEmpty(serviceInfo?.Endpoint))
            {
                return null;
            }

            return await CallServiceInternal(serviceInfo, cacheRegion, cancellationToken, additionalParameters);
        }

        protected abstract Task<ServiceResponse> CallServiceInternal(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> additionalParameters);
    }
}