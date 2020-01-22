namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// A called service wrapper, where the called service should be called every time wrapper is called.
    /// </summary>
    public interface IDynamicService : IServiceBase
    {
    }
}