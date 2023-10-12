using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Dependency Injection settings describing how the Refit client should be configured.
/// This can be used to configure the HttpClient pipeline with additional handlers
/// </summary>
public class DependencyInjectionSettings
{
    /// <summary>
    /// Base Address for the HttpClient
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// A collection of HttpMessageHandlers to be added to the HttpClient pipeline.
    /// This can be for telemetry logging, authorization, etc.
    /// </summary>
    [JsonPropertyName("httpMessageHandlers")]
    public string[] HttpMessageHandlers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Set this to true to use Polly for transient fault handling.
    /// </summary>
    [JsonPropertyName("usePolly")]
    public bool UsePolly { get; set; }

    /// <summary>
    /// Default max retry count for Polly. Default is 6.
    /// </summary>
    [JsonPropertyName("pollyMaxRetryCount")]
    public int PollyMaxRetryCount { get; set; } = 6;

    /// <summary>
    /// The median delay to target before the first retry in seconds. Default is 1 second
    /// </summary>
    [JsonPropertyName("firstBackoffRetryInSeconds")]
    public double FirstBackoffRetryInSeconds { get; set; } = 1.0;
}