using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    public class ServiceInfo
    {
        public Guid Id { get; set; }
        public string CacheKey => $"{Id}";
        public string Name { get; set; }
        public TimeSpan BreakDuration { get; set; }
        public string Endpoint { get; set; }
        public int ExceptionCount { get; set; }
        public TimeSpan Timeout { get; set; }
        public IEnumerable<TokenInfo> Tokens { get; set; }
        public IEnumerable<ParameterInfo> AdditionalParameters { get; set; }
    }
}