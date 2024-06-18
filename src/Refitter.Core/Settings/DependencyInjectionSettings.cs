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
    public string? BaseUrl { get; set; }

    /// <summary>
    /// A collection of HttpMessageHandlers to be added to the HttpClient pipeline.
    /// This can be for telemetry logging, authorization, etc.
    /// </summary>
    public string[] HttpMessageHandlers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Set this to true to use Polly for transient fault handling.
    /// </summary>
    [Obsolete("Use HandleTransientErrors instead", true)]
    public bool UsePolly { get; set; }
    
    /// <summary>
    /// Set this to true to implement transient fault handling.
    /// </summary>
    public TransientErrorHandler HandleTransientErrors { get; set; } = TransientErrorHandler.None;

    /// <summary>
    /// Default max retry count for Polly. Default is 6.
    /// </summary>
    public int PollyMaxRetryCount { get; set; } = 6;

    /// <summary>
    /// The median delay to target before the first retry in seconds. Default is 1 second
    /// </summary>
    public double FirstBackoffRetryInSeconds { get; set; } = 1.0;

    /// <summary>
    /// Name of IServiceCollection Extension Method. Default is ConfigureRefitClients
    /// </summary>
    public string ExtensionMethodName { get; set; } = "ConfigureRefitClients";
}