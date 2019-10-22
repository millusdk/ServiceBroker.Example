namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// An entry in the class
    /// </summary>
    /// <typeparam name="T">The type of the element in the cache</typeparam>
    public class CacheEntry<T> where T : class
    {
        /// <summary>
        /// The value of the entry in the cache
        /// </summary>
        public T Value { get; set; }
    }
}