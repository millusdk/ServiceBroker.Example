using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// A service for calling external HTTP-resources.
    /// </summary>
    public interface IHttpClientWrapper
    {
        /// <summary>
        /// Sends an async post request to the specified endpoint. Attaches the specified post parameters to the body of the request.
        /// </summary>
        /// <param name="endpoint">The endpoint to post the request to.</param>
        /// <param name="postParameters">The post parameters included in the body of the request.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the request.</param>
        /// <returns>A HttpClientResponse representing the result of the post request.</returns>
        Task<HttpClientResponse> PostAsync(string endpoint, IEnumerable<KeyValuePair<string, string>> postParameters, CancellationToken cancellationToken);
    }
}