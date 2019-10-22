using System;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Information about additional tokens to include as post parameters in the body of the request to a service
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// The id of the token to include
        /// </summary>
        public Guid TokenId { get; set; }

        /// <summary>
        /// The name of the post parameter to include
        /// </summary>
        public string Name { get; set; }
    }
}