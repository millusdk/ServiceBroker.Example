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

        void StartBackgroundServiceCalls(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout);
    }
}