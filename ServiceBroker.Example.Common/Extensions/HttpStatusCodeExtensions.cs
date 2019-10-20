using System.Net;

namespace ServiceBroker.Example.Common.Extensions
{
    public static class HttpStatusCodeExtensions
    {
        public static bool IsOkStatus(this HttpStatusCode httpStatusCode)
        {
            switch (httpStatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NonAuthoritativeInformation:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.ResetContent:
                case HttpStatusCode.PartialContent:
                    return true;
                default:
                    return false;
            }
        }
    }
}