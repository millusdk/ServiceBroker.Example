using System.Net;

namespace ServiceBroker.Example.Models
{
    public class HttpClientResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string Response { get; set; }
    }
}