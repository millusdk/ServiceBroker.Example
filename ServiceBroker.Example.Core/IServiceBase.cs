using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface IServiceBase
    {
        Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion,
            CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters);
    }
}