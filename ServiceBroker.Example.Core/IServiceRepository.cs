using System;
using System.Collections.Generic;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface IServiceRepository
    {
        IEnumerable<ServiceInfo> GetServicesAndTokens(IEnumerable<Guid> serviceAndTokenIds);

        IEnumerable<CachedServiceInfo> GetCachedServices();
    }
}
