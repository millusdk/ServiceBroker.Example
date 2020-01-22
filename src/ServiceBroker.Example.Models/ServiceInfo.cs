using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Information about a service
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// The id of the service
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
        /// The tokens to evaluate on the response from the service
        /// </summary>
        public IEnumerable<TokenInfo> Tokens { get; set; }

        /// <summary>
        /// Indicates if the data from the service should be hidden from visualizations of the user profile
        /// </summary>
        public bool ExcludeFromVisibleUserProfile { get; set; }
    }
}