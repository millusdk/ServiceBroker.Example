using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    public interface IHttpClientWrapper
    {
        Task<HttpClientResponse> PostAsync(string endpoint, IEnumerable<KeyValuePair<string, string>> postPrameters, CancellationToken cancellationToken);
    }
}