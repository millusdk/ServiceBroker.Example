using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    /// <summary>
    /// Base implementation for all service types
    /// </summary>
    public abstract class ServiceBase: IServiceBase
    {
        /// <summary>
        /// Retrieves data from the service specified in the service info parameter
        /// </summary>
        /// <param name="serviceInfo">Information about the service to call</param>
        /// <param name="cacheRegion">The cache region to use for cache lookups</param>
        /// <param name="cancellationToken">Cancellation token to cancel the request</param>
        /// <param name="additionalParameters">Additional post parameters to include in the request body</param>
        /// <returns>Service response with the result of the data retrieval</returns>
        public abstract Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters);
    }
}