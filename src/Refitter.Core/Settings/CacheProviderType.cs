namespace Refitter.Core
{
    /// <summary>
    /// Enumeration of cache provider types.
    /// </summary>
    public enum CacheProviderType
    {
        /// <summary>
        /// No cache provider.
        /// </summary>
        None,
        
        /// <summary>
        /// Akavache cache provider.
        /// </summary>
        Akavache,
        
        /// <summary>
        /// MonkeyCache cache provider.
        /// </summary>
        MonkeyCache,
        
        /// <summary>
        /// In-memory cache provider.
        /// </summary>
        InMemory,
        
        /// <summary>
        /// Distributed cache as string provider.
        /// </summary>
        DistributedAsString,
        
        /// <summary>
        /// Distributed cache as byte array provider.
        /// </summary>
        DistributedAsByteArray,
    }
}