using System.Diagnostics.CodeAnalysis;

namespace ServiceBroker.Example.Common
{
    /// <summary>
    /// A set of constants used throughout the application
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Constants
    {
        /// <summary>
        /// Represents the cache key to look for the user identifier under
        /// </summary>
        public static readonly string UserIdentifierCacheKey = "BorgerId";

        /// <summary>
        /// Represents the name of the post parameter to send the user identifier as
        /// </summary>
        public static readonly string UserIdentifierPostKey = "Cpr";
    }
}