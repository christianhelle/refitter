namespace Refitter.Core.Validation;

/// <summary>
/// The counts of the elements found in an OpenAPI document.
/// </summary>
public class OpenApiStats
{
    /// <summary>
    /// Gets or sets the number of parameters found.
    /// </summary>
    public int ParameterCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of schemas found.
    /// </summary>
    public int SchemaCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of headers found.
    /// </summary>
    public int HeaderCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of path items found.
    /// </summary>
    public int PathItemCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of request bodies found.
    /// </summary>
    public int RequestBodyCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of responses found.
    /// </summary>
    public int ResponseCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of operations found.
    /// </summary>
    public int OperationCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of links found.
    /// </summary>
    public int LinkCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of callbacks found.
    /// </summary>
    public int CallbackCount { get; set; } = 0;

    /// <summary>
    /// Returns a formatted string with all element counts.
    /// </summary>
    public override string ToString()
    {
        return $"""
                 - Path Items: {PathItemCount}
                 - Operations: {OperationCount}
                 - Parameters: {ParameterCount}
                 - Request Bodies: {RequestBodyCount}
                 - Responses: {ResponseCount}
                 - Links: {LinkCount}
                 - Callbacks: {CallbackCount}
                 - Schemas: {SchemaCount}
                """;
    }
}
