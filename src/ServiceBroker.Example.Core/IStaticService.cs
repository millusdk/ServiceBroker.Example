namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// A service that always returns the same answer no matter which user the service is called for
    /// </summary>
    public interface IStaticService : IServiceBase
    {
        
    }
}