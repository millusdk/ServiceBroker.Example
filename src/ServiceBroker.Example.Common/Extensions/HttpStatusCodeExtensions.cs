using System.Net;

namespace ServiceBroker.Example.Common.Extensions
{
    /// <summary>
    /// Extensions for the HttpStatusCode enum
    /// </summary>
    public static class HttpStatusCodeExtensions
    {
        /// <summary>
        /// Returns whether the status code is a 2XX code
        /// </summary>
        /// <param name="httpStatusCode"></param>
        /// <returns></returns>
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