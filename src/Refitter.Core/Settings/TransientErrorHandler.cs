using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// Libraries for handling transient errors
/// </summary>
[Description("Libraries for handling transient errors")]
public enum TransientErrorHandler
{
    /// <summary>
    /// No transient error handling
    /// </summary>
    [Description("No transient error handling")]
    None,

    /// <summary>
    /// Use Polly for transient fault handling (Deprecated)
    /// </summary>
    [Description("Use Polly for transient fault handling (Deprecated)")]
    Polly,

    /// <summary>
    /// Use Microsoft.Extensions.Http.Resilience for transient fault handling
    /// </summary>
    [Description("Use Microsoft.Extensions.Http.Resilience for transient fault handling")]
    HttpResilience
}

