using System;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents the the result of a token evaluation
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// The id of the token
        /// </summary>
        public Guid TokenId { get; set; }

        /// <summary>
        /// The cache key for the token
        /// </summary>
        public string CacheKey => $"{TokenId}";

        /// <summary>
        /// The status of the valuation of the service
        /// </summary>
        public TokenResponseStatus Status { get; set; }

        /// <summary>
        /// The result of the evaluation of the token
        /// </summary>
        public string Value { get; set; }
    }
}