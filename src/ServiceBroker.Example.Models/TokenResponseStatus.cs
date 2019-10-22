namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// The status of the evaluation of a token
    /// </summary>
    public enum TokenResponseStatus
    {
        /// <summary>
        /// The evaluation of the token resulted in a value
        /// </summary>
        Found,

        /// <summary>
        /// The evaluation of the token resulted in no value
        /// </summary>
        NotFound,

        /// <summary>
        /// An error occured during evaluation of the token
        /// </summary>
        Error
    }
}