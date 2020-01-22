using System.Collections.Generic;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// A response from the service broker service
    /// </summary>
    public class ServiceBrokerResponse
    {
        /// <summary>
        /// The service responses from the services
        /// </summary>
        public IEnumerable<ServiceResponse> ServiceResponses { get; set; }
    }
}