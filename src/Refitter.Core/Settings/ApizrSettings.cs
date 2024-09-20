using System.Text.Json.Serialization;

namespace Refitter.Core
{
    /// <summary>
    /// Describing how Apizr should be configured.
    /// Here are only the common configurations.
    /// </summary>
    public class ApizrSettings
    {
        /// <summary>
        /// Set it to true to include an Apizr Request Options parameter into all api methods
        /// and get all the Apizr goodness (default: true)
        /// </summary>
        public bool WithRequestOptions { get; set; } = true;

        /// <summary>
        /// Set it to true to generate an Apizr registration helper ready to use (default: false).
        /// Please note that it will generate an extended or static helper depending on DependencyInjectionSettings property value.
        /// </summary>
        public bool WithRegistrationHelper { get; set; } = false;

        /// <summary>
        /// Library to use for cache handling (default: None)
        /// Options:
        /// - None
        /// - Akavache
        /// - MonkeyCache
        /// - InMemory (Microsoft.Extensions.Caching.Memory)
        /// - Distributed (Microsoft.Extensions.Caching.Distributed)
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CacheProviderType WithCacheProvider { get; set; }

        /// <summary>
        /// Library to use for data mapping handling (default: None)
        /// Options:
        /// - None
        /// - AutoMapper
        /// - Mapster
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MappingProviderType WithMappingProvider { get; set; }

        /// <summary>
        /// Set it to true to handle request with priority (default: false)
        /// </summary>
        public bool WithPriority { get; set; } = false;

        /// <summary>
        /// Set it to true to handle request with MediatR (default: false)
        /// </summary>
        public bool WithMediation { get; set; } = false;

        /// <summary>
        /// Set it to true to handle request with MediatR and Optional result (default: false)
        /// </summary>
        public bool WithOptionalMediation { get; set; } = false;

        /// <summary>
        /// Set it to true to manage file transfers (default: false)
        /// </summary>
        public bool WithFileTransfer { get; set; } = false;
    }
}
