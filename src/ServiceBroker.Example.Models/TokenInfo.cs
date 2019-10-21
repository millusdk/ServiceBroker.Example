using System;

namespace ServiceBroker.Example.Models
{
    public class TokenInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CacheKey => $"{Id}";
    }
}