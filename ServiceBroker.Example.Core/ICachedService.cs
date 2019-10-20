namespace ServiceBroker.Example.Core
{
    public interface ICachedService : IServiceBase
    {
        bool RemoveSemaphores(string cacheRegion);
    }
}