using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface ICache
    {
        CacheEntry<T> Get<T>(string cacheRegion, string key) where T : class;
        void Set<T>(string cacheRegion, string key, T value) where T : class;

        void ClearRegion(string cacheRegion);
    }
}