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
    /// <summary>
    /// An abstract service used as the base implementation for the different service types
    /// </summary>
    public abstract class CalledServiceBase : ServiceBase
    {
        private static readonly ConcurrentDictionary<string, AsyncPolicyWrap<ServiceResponse>> CircuitBreakerPolicies =
            new ConcurrentDictionary<string, AsyncPolicyWrap<ServiceResponse>>();

        protected readonly ICache Cache;

        /// <summary>
        /// Log used to write information about the result of the requests. Defaults to a null logger.
        /// </summary>
        [ExcludeFromCodeCoverage]
        // ReSharper disable once MemberCanBeProtected.Global
        public ILogger Log { get; set; } = Logger.None;

        protected CalledServiceBase(ICache cache)
        {
            Cache = cache;
        }

        /// <summary>
        /// Generates a list of KeyValuePairs to use as the post body for the request to a called service
        /// </summary>
        /// <param name="serviceInfo">Information about the service to generate parameters for</param>
        /// <param name="cacheRegion">The cache region to look for values for post parameters under in</param>
        /// <param name="additionalParameters">Additional post parameters to include in the generated list in addition to the ones defined on the service info</param>
        /// <returns>List of key value pairs to use as post parameters in the body of a request</returns>
        public IEnumerable<KeyValuePair<string, string>> GetPostParameters(CalledServiceInfo serviceInfo, string cacheRegion,
            IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            List<KeyValuePair<string, string>> pairs = additionalParameters?.ToList() ?? new List<KeyValuePair<string, string>>();

            CacheEntry<string> userIdentifier = Cache.Get<string>(cacheRegion, Common.Constants.UserIdentifierCacheKey);

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
                    parameterValue = Cache.Get<string>(cacheRegion, additionalParameter.TokenId.ToString())?.Value
                })
                .Where(additionalParameter => !string.IsNullOrEmpty(additionalParameter.parameterValue))
                .Select(additionalParameter => new KeyValuePair<string, string>(additionalParameter.additionalParameter.Name, additionalParameter.parameterValue))
                .ToList();
            pairs.AddRange(
                additionalUserParameters
                );

            return pairs;
        }

        /// <summary>
        /// Generates the circuit breaker policy for a service.
        /// </summary>
        /// <param name="serviceInfo">The service to generate the circuit breaker policy for</param>
        /// <returns>The circuit policy</returns>
        protected AsyncPolicyWrap<ServiceResponse> GetCircuitBreakerPolicy(CalledServiceInfo serviceInfo)
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
                        handledEventsAllowedBeforeBreaking: serviceInfo.CircuitBreakerInfo.ExceptionCount,
                        durationOfBreak: serviceInfo.CircuitBreakerInfo.BreakDuration,
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
                                ServiceId = serviceInfo.Id
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
                                ServiceId = serviceInfo.Id
                            });
                        });
                AsyncPolicyWrap<ServiceResponse> exceptionFallbackPolicyWrappingTimeoutFallback = exceptionFallbackPolicy
                    .WrapAsync(timeoutFallbackPolicyWrappingCircuitBreaker);

                AsyncPolicyWrap<ServiceResponse> policy = exceptionFallbackPolicyWrappingTimeoutFallback;

                return policy;
            });
        }

        /// <summary>
        /// Calls the service specified in the service info parameter.
        /// </summary>
        /// <param name="serviceInfo">Information about the service to call</param>
        /// <param name="cacheRegion">The cache region to use for cache lookups</param>
        /// <param name="cancellationToken">Cancellation token to cancel the request</param>
        /// <param name="additionalParameters">Additional post parameters to include in the request body</param>
        /// <returns>Service response with the result of the call</returns>
        public override async Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            if (!(serviceInfo is CalledServiceInfo calledServiceInfo))
            {
                return null;
            }

            if (string.IsNullOrEmpty(calledServiceInfo.Endpoint))
            {
                return null;
            }

            return await CallServiceInternal(calledServiceInfo, cacheRegion, cancellationToken, additionalParameters);
        }

        protected abstract Task<ServiceResponse> CallServiceInternal(CalledServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> additionalParameters);
    }
}