using System;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents a token
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// The id of the token
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the token
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The cache key for the token
        /// </summary>
        public string CacheKey => $"{Id}";
    }
}