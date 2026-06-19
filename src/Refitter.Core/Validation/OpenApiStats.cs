using Microsoft.OpenApi;

namespace Refitter.Core.Validation;

/// <summary>
/// Walks an OpenAPI document and collects counts of its elements.
/// </summary>
public class OpenApiStats : OpenApiVisitorBase
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
    /// Increments <see cref="ParameterCount"/>.
    /// </summary>
    public override void Visit(IOpenApiParameter parameter)
    {
        ParameterCount++;
    }

    /// <summary>
    /// Increments <see cref="SchemaCount"/>.
    /// </summary>
    public override void Visit(IOpenApiSchema schema)
    {
        SchemaCount++;
    }

    /// <summary>
    /// Increments <see cref="HeaderCount"/>.
    /// </summary>
    public override void Visit(IDictionary<string, IOpenApiHeader> headers)
    {
        HeaderCount++;
    }

    /// <summary>
    /// Increments <see cref="PathItemCount"/>.
    /// </summary>
    public override void Visit(IOpenApiPathItem pathItem)
    {
        PathItemCount++;
    }

    /// <summary>
    /// Increments <see cref="RequestBodyCount"/>.
    /// </summary>
    public override void Visit(IOpenApiRequestBody requestBody)
    {
        RequestBodyCount++;
    }

    /// <summary>
    /// Increments <see cref="ResponseCount"/>.
    /// </summary>
    public override void Visit(OpenApiResponses response)
    {
        ResponseCount++;
    }

    /// <summary>
    /// Increments <see cref="OperationCount"/>.
    /// </summary>
    public override void Visit(OpenApiOperation operation)
    {
        OperationCount++;
    }

    /// <summary>
    /// Increments <see cref="LinkCount"/>.
    /// </summary>
    public override void Visit(IOpenApiLink link)
    {
        LinkCount++;
    }

    /// <summary>
    /// Increments <see cref="CallbackCount"/>.
    /// </summary>
    public override void Visit(IOpenApiCallback callback)
    {
        CallbackCount++;
    }

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
