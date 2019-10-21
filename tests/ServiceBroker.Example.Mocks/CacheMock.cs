using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Mocks
{
    public class CacheMock : ICache
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public void ClearRegion(string cacheRegion)
        {
            throw new NotImplementedException();
        }

        public CacheEntry<T> Get<T>(string cacheRegion, string key) where T : class
        {
            if(_cache.TryGetValue($"{cacheRegion}-{key}", out object value))
            {
                return new CacheEntry<T>
                {
                    Value = value as T
                };
            }

            return null;
        }

        public void Set<T>(string cacheRegion, string key, T value) where T : class
        {
            _cache.Add($"{cacheRegion}-{key}", value);
        }
    }
}
