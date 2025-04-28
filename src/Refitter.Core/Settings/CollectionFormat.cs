namespace Refitter.Core;

/// <summary>
/// Collection format defined in https://swagger.io/docs/specification/v2_0/describing-parameters/#array-and-multi-value-parameters
/// </summary>
public enum CollectionFormat
{
    /// <summary>
    /// Multiple parameter instances rather than multiple values. 
    /// Only supported for the in: query and in: formData parameters. 
    /// Example: ?param=value1&param=value2&param=value3
    /// </summary>
    Multi,

    /// <summary>
    /// Comma-separated values
    /// Example: param=value1,value2,value3
    /// </summary>
    Csv,

    /// <summary>
    /// Space-separated values
    /// Example: param=value1 value2 value3
    /// </summary>
    Ssv,

    /// <summary>
    /// Tab-separated values
    /// Example: param=value1\tvalue2\tvalue3
    /// </summary>
    Tsv,

    /// <summary>
    /// Pipe-separated values
    /// Example: param=value1|value2|value3
    /// </summary>
    Pipes
}
