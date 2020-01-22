namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// The status of the response from the service
    /// </summary>
    public enum ServiceResponseStatus
    {
        /// <summary>
        /// The call was a success
        /// </summary>
        Success,

        /// <summary>
        /// The call resulted in an error
        /// </summary>
        Error,

        /// <summary>
        /// The call resulted in a timeout
        /// </summary>
        Timeout
    }
}