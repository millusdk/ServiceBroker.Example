using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    public class StaticService : ServiceBase, IStaticService
    {
        public override Task<ServiceResponse> CallService(ServiceInfo serviceInfo, string cacheRegion, CancellationToken cancellationToken, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            throw new System.NotImplementedException();
        }
    }
}