using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    public class CalledServiceInfo : ServiceInfo
    {
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
        /// Additional parameters to include as post parameters to the request
        /// </summary>
        public IEnumerable<ParameterInfo> AdditionalParameters { get; set; }
    }
}