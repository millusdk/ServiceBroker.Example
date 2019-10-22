using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents the response from a service
    /// </summary>
    public class ServiceResponse
    {
        /// <summary>
        /// The id of the service called
        /// </summary>
        public Guid ServiceId { get; set; }

        /// <summary>
        /// The status for the call to the service
        /// </summary>
        public ServiceResponseStatus Status { get; set; }

        /// <summary>
        /// The evaluated tokens
        /// </summary>
        public IEnumerable<TokenResponse> TokenResponses { get; set; }

        /// <summary>
        /// The value of the response from the service
        /// </summary>
        public string Value { get; set; }
    }
}