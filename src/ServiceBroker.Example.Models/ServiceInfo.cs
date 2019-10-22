using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Information about an external service
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// The id of the external service
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The cache key used to store the response under if needed
        /// </summary>
        public string CacheKey => $"{Id}";

        /// <summary>
        /// The name of the service. Also used as the name of the wrapping element in the user profile
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contains information about how to handle circuit breaker functionality for the service
        /// </summary>
        public CircuitBreakerInfo CircuitBreakerInfo { get; set; } = new CircuitBreakerInfo();

        /// <summary>
        /// The endpoint to call
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// How log to wait before timing the call out
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// The tokens to evaluate on the response from the extrenal service
        /// </summary>
        public IEnumerable<TokenInfo> Tokens { get; set; }

        /// <summary>
        /// Additional parameters to include as post parameters to the request
        /// </summary>
        public IEnumerable<ParameterInfo> AdditionalParameters { get; set; }
    }
}