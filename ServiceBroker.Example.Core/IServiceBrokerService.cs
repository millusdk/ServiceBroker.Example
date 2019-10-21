using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface IServiceBrokerService
    {
        /// <summary>
        /// Calls the services specified by the service and token ids, and returns the results of these calls.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the services should wait for answers from external services.</param>
        /// <returns>An object representing the result of the calls to the services specified by the service and token ids.</returns>
        ServiceBrokerResponse CallServices(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);

        /// <summary>
        /// Calls the services specified by the service and token ids, and returns the results of these calls.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from external services.</param>
        /// <returns>An object representing the result of the calls to the services specified by the service and token ids.</returns>
        Task<ServiceBrokerResponse> CallServicesAsync(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);

        /// <summary>
        /// Calls the specified service, and returns the result of the call.
        /// </summary>
        /// <param name="serviceId">The id of the service to call.</param>
        /// <param name="cacheRegion">he region to use for storing and retrieving values, if required by the service called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from service.</param>
        /// <param name="additionalParameters">Specification of additional parameters to include in the body of any outgoing requests performed as part of the processing of the service.</param>
        /// <returns>An object representing the result of the call to the external service.</returns>
        ServiceResponse CallService(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters);

        /// <summary>
        /// Calls the specified service, and returns the result of the call.
        /// </summary>
        /// <param name="serviceId">The id of the service to call.</param>
        /// <param name="cacheRegion">he region to use for storing and retrieving values, if required by the service called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from service.</param>
        /// <param name="additionalParameters">Specification of additional parameters to include in the body of any outgoing requests performed as part of the processing of the service.</param>
        /// <returns>An object representing the result of the call to the external service.</returns>
        Task<ServiceResponse> CallServiceAsync(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters);

        /// <summary>
        /// Starts call to the services specified by the service and token ids on a background thread.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the services should wait for answers from external services.</param>
        void StartBackgroundServiceCalls(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);
    }
}