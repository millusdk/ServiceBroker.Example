using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    public class ServiceBrokerResponse
    {
        public IEnumerable<ServiceResponse> ServiceResponses { get; set; }
    }
}