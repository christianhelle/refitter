using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Dependency Injection settings describing how the Refit client should be configured.
/// This can be used to configure the HttpClient pipeline with additional handlers
/// </summary>
public class DependencyInjectionSettings
{
    public const string DefaultExtensionMethodName = "ConfigureRefitClients";

    /// <summary>
    /// Base Address for the HttpClient
    /// </summary>
    [Description("Base Address for the HttpClient")]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// A collection of HttpMessageHandlers to be added to the HttpClient pipeline.
    /// This can be for telemetry logging, authorization, etc.
    /// </summary>
    [Description(
        """
        A collection of HttpMessageHandlers to be added to the HttpClient pipeline.
        This can be for telemetry logging, authorization, etc.
        """
    )]
    public string[] HttpMessageHandlers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Set this to true to use Polly for transient fault handling.
    /// This is deprecated. Use TransientErrorHandler instead.
    /// </summary>
    [Description(
        """
        Set this to true to use Polly for transient fault handling.
        This is deprecated. Use TransientErrorHandler instead.
        """
    )]
    [Obsolete("Use TransientErrorHandler instead")]
    public bool UsePolly
    {
        get => TransientErrorHandler == TransientErrorHandler.Polly;
        set => TransientErrorHandler = value ? TransientErrorHandler.Polly : TransientErrorHandler.None;
    }

    /// <summary>
    /// Library to use for transient error handling
    /// Options:
    /// - None
    /// - Polly - Polly Framework and HTTP Extensions
    /// - HttpResilience - Microsoft HTTP Resilience Library
    /// </summary>
    [Description(
        """
        Library to use for transient error handling
        Options:
        - None
        - Polly - Polly Framework and HTTP Extensions
        - HttpResilience - Microsoft HTTP Resilience Library
        """
    )]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransientErrorHandler TransientErrorHandler { get; set; }

    /// <summary>
    /// Default max retry count for transient error handling. Default is 6.
    /// This is deprecated. Use MaxRetryCount instead.
    /// </summary>
    [Description(
        """
        Default max retry count for transient error handling. Default is 6.
        This is deprecated. Use MaxRetryCount instead.
        """
    )]
    [Obsolete("Use MaxRetryCount instead")]
    public int PollyMaxRetryCount
    {
        get => MaxRetryCount;
        set => MaxRetryCount = value;
    }

    /// <summary>
    /// Default max retry count for transient error handling. Default is 6.
    /// </summary>
    [Description("Default max retry count for transient error handling. Default is 6.")]
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>
    /// The median delay to target before the first retry in seconds. Default is 1 second
    /// </summary>
    [Description("The median delay to target before the first retry in seconds. Default is 1 second")]
    public double FirstBackoffRetryInSeconds { get; set; } = 1.0;

    /// <summary>
    /// Name of IServiceCollection Extension Method. Default is ConfigureRefitClients, or ConfigureApizrManagers if ApizrSettings property is set
    /// </summary>
    [Description(
        """
        Name of IServiceCollection Extension Method.
        Default is ConfigureRefitClients, or ConfigureApizrManagers if ApizrSettings property is set
        """
    )]
    public string ExtensionMethodName { get; set; } = DefaultExtensionMethodName;
}
