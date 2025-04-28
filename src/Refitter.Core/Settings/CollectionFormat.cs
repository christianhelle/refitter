namespace Refitter.Core;

/// <summary>
/// Collection format defined in https://swagger.io/docs/specification/v2_0/describing-parameters/#array-and-multi-value-parameters
/// </summary>
public enum CollectionFormat
{
    /// <summary>
    /// Multiple parameter instances
    /// </summary>
    Multi,

    /// <summary>
    /// Comma-separated values
    /// </summary>
    Csv,

    /// <summary>
    /// Space-separated values
    /// </summary>
    Ssv,

    /// <summary>
    /// Tab-separated values
    /// </summary>
    Tsv,

    /// <summary>
    /// Pipe-separated values
    /// </summary>
    Pipes
}
