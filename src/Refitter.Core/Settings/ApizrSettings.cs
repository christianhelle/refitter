using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Refitter.Core
{
    /// <summary>
    /// Describing how Apizr should be configured.
    /// Here are only the common configurations.
    /// </summary>
    [Description("Describing how Apizr should be configured. Here are only the common configurations.")]
    public class ApizrSettings
    {
        /// <summary>
        /// Set it to true to include an Apizr Request Options parameter into all api methods
        /// and get all the Apizr goodness (default: true)
        /// </summary>
        [Description(
            """
            Set it to true to include an Apizr Request Options parameter into all api methods
            and get all the Apizr goodness (default: true)
            """
        )]
        public bool WithRequestOptions { get; set; } = true;

        /// <summary>
        /// Set it to true to generate an Apizr registration helper ready to use (default: false).
        /// Please note that it will generate an extended or static helper depending on DependencyInjectionSettings property value.
        /// </summary>
        [Description(
            """
            Set it to true to generate an Apizr registration helper ready to use (default: false).
            Please note that it will generate an extended or static helper depending on DependencyInjectionSettings property value.
            """
        )]
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
        [Description(
            """
            Library to use for cache handling (default: None)
            Options:
            - None
            - Akavache
            - MonkeyCache
            - InMemory (Microsoft.Extensions.Caching.Memory)
            - Distributed (Microsoft.Extensions.Caching.Distributed)
            """
        )]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CacheProviderType WithCacheProvider { get; set; }

        /// <summary>
        /// Library to use for data mapping handling (default: None)
        /// Options:
        /// - None
        /// - AutoMapper
        /// - Mapster
        /// </summary>
        [Description(
            """
            Library to use for data mapping handling (default: None)
            Options:
            - None
            - AutoMapper
            - Mapster
            """
        )]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MappingProviderType WithMappingProvider { get; set; }

        /// <summary>
        /// Set it to true to handle request with priority (default: false)
        /// </summary>
        [Description("Set it to true to handle request with priority (default: false)")]
        public bool WithPriority { get; set; } = false;

        /// <summary>
        /// Set it to true to handle request with MediatR (default: false)
        /// </summary>
        [Description("Set it to true to handle request with MediatR (default: false)")]
        public bool WithMediation { get; set; } = false;

        /// <summary>
        /// Set it to true to manage file transfers (default: false)
        /// </summary>
        [Description("Set it to true to manage file transfers (default: false)")]
        public bool WithFileTransfer { get; set; } = false;
    }
}
