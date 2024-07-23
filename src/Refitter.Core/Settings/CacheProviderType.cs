namespace Refitter.Core
{
    public enum CacheProviderType
    {
        None,
        Akavache,
        MonkeyCache,
        InMemory,
        DistributedAsString,
        DistributedAsByteArray,
    }
}