using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface IServiceBrokerService
    {
        ServiceBrokerResponse CallServices(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);

        Task<ServiceBrokerResponse> CallServicesAsync(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);

        ServiceResponse CallService(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters);

        Task<ServiceResponse> CallServiceAsync(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters);

        void StartBackgroundServiceCalls(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);
    }
}