namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// An external service wrapper, where the external service should be called every time wrapper is called.
    /// </summary>
    public interface IDynamicService : IServiceBase
    {
    }
}