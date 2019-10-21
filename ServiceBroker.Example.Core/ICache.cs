using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// Represents an instance of a cache framework.
    /// The interface is an abstraction exposing only the subset of features from a caching framework needed by the implemented services of the example.
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Attempts to retrieve an element under the specified cache key in the specified cache region.
        /// </summary>
        /// <typeparam name="T">Type or base type of the element to be retrieved</typeparam>
        /// <param name="cacheRegion">The region of the cache to retrieve the key from</param>
        /// <param name="key">The key to look for in the region</param>
        /// <returns>The cache entry representing the entry in the cache. Null if no entry is found in the specified region.</returns>
        CacheEntry<T> Get<T>(string cacheRegion, string key) where T : class;

        /// <summary>
        /// Sets the value of the cache entry for the specified combination of cache region and cache key.
        /// </summary>
        /// <typeparam name="T">The type of the object stored under the cache key and cache region.</typeparam>
        /// <param name="cacheRegion">The region of the cache to store the entry in.</param>
        /// <param name="key">The key of the entry in the cache region.</param>
        /// <param name="value">The value to be stored under the combination of cache region and cache key</param>
        void Set<T>(string cacheRegion, string key, T value) where T : class;

        /// <summary>
        /// Clears all cache keys under the specified cache region.
        /// </summary>
        /// <param name="cacheRegion">The cache region to be cleared.</param>
        void ClearRegion(string cacheRegion);
    }
}