using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;
using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Mocks
{
    public class CacheMock : ICache
    {
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        public void ClearRegion(string cacheRegion)
        {
            throw new NotImplementedException();
        }

        public CacheEntry Get(string cacheRegion, string key)
        {
            if(_cache.TryGetValue($"{cacheRegion}-{key}", out string value))
            {
                return new CacheEntry
                {
                    Value = value
                };
            }

            return null;
        }

        public void Set(string cacheRegion, string key, string value)
        {
            _cache.Add($"{cacheRegion}-{key}", value);
        }
    }
}
