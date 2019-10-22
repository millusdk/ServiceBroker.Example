namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Contains information about a service which stores data such that repeated calls do not result in additional requests to the external service.
    /// </summary>
    public class CachedServiceInfo : ServiceInfo
    {
    }
}