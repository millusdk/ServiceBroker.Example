using System.Diagnostics.CodeAnalysis;

namespace ServiceBroker.Example.Common
{
    [ExcludeFromCodeCoverage]
    public static class Constants
    {
        public static readonly string UserIdentifierCacheKey = "BorgerId";
        public static readonly string UserIdentifierPostKey = "Cpr";
    }
}