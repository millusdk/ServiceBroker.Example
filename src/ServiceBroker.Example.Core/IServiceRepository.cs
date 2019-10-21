using System;
using System.Collections.Generic;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// This interface represents a repository tha tis able to load data about one or more external services and/or tokens from a data source.
    /// </summary>
    public interface IServiceRepository
    {
        /// <summary>
        /// Retrieves information about the services needed to satisfy the service and token ids.
        /// </summary>
        /// <param name="serviceAndTokenIds">The ids of the services and tokens to retrieve information about</param>
        /// <returns>A list of services needed to satisfy the specified service and token id</returns>
        IEnumerable<ServiceInfo> GetServicesAndTokens(IEnumerable<Guid> serviceAndTokenIds);

        /// <summary>
        /// Retrieves all cached services and tokens from the database.
        /// </summary>
        /// <returns>A list of all cached services</returns>
        IEnumerable<CachedServiceInfo> GetCachedServices();
    }
}
