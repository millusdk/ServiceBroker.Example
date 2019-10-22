namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents an XSLT-token
    /// </summary>
    public class XsltTokenInfo : TokenInfo
    {
        /// <summary>
        /// The XSLT to execute
        /// </summary>
        public string Xslt { get; set; }
    }
}