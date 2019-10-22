namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Represents an XPath token
    /// </summary>
    public class XPathTokenInfo : TokenInfo
    {
        /// <summary>
        /// The XPath to evaluate
        /// </summary>
        public string XPath { get; set; }
    }
}