using System;

namespace ServiceBroker.Example.Models
{
    public class TokenResponse
    {
        public Guid Id { get; set; }
        public string CacheKey => $"{Id}";
        public TokenResponseStatus Status { get; set; }
        public string Value { get; set; }
    }
}