using System;
using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    public class ServiceResponse
    {
        public Guid Id { get; set; }
        public ServiceResponseStatus Status { get; set; }
        public IEnumerable<TokenResponse> TokenResponses { get; set; }
        public string Value { get; set; }
    }
}