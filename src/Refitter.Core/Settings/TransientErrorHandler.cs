namespace Refitter.Core;

/// <summary>
/// Libraries for handling transient errors
/// </summary>
public enum TransientErrorHandler
{
    /// <summary>
    /// No transient error handling
    /// </summary>
    None,
    /// <summary>
    /// Use Polly for transient fault handling (Deprecated)
    /// </summary>
    Polly,
    /// <summary>
    /// Use Microsoft.Extensions.Http.Resilience for transient fault handling
    /// </summary>
    HttpResilience
}