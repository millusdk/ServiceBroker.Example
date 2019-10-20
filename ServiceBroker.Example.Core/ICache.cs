using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface ICache
    {
        CacheEntry Get(string cacheRegion, string key);
        void Set(string cacheRegion, string key, string value);

        void ClearRegion(string cacheRegion);
    }
}