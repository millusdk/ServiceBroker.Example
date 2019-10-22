using System.Net;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents a response from an HTTP call
    /// </summary>
    public class HttpClientResponse
    {
        /// <summary>
        /// The status code returned from the external service
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// /The response body returned from the external service
        /// </summary>
        public string Response { get; set; }
    }
}