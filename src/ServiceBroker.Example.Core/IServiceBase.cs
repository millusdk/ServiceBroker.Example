using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// A base interface used for all external service types. Exposes the methods that must be implemented by all external service types.
    /// </summary>
    public interface IServiceBase
    {
        /// <summary>
        /// Calls the specified service if required based on cache and service type, and returns the result of the call.
        /// The result also includes the values of any tokens included in the service info-object.
        /// </summary>
        /// <param name="serviceInfo">The service to be called</param>
        /// <param name="cacheRegion">The cache region to store/retrieve values from if applicable</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the request to the service.</param>
        /// <param name="additionalParameters">Additional parameters to include in the request if they have a value available in the cache.</param>
        /// <returns>The result of the call to the service or the value retrieved from the cache.</returns>
        Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters);
    }
}