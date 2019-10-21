namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// An external service wrapper which saves the values returned from the external service in the cache, such that consecutive calls within the same cache region reads data from the cache rather than call the external service.
    /// </summary>
    public interface ICachedService : IServiceBase
    {
        /// <summary>
        /// Removes all cached semaphores (lock objects) stored under the specified cache region.
        /// </summary>
        /// <param name="cacheRegion">The cache region to remove semaphores for</param>
        /// <returns></returns>
        bool RemoveSemaphores(string cacheRegion);
    }
}