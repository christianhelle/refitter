using Microsoft.OpenApi;

namespace Refitter.Validation;

public class OpenApiStats : OpenApiVisitorBase
{
    public int ParameterCount { get; set; } = 0;
    public int SchemaCount { get; set; } = 0;
    public int HeaderCount { get; set; } = 0;
    public int PathItemCount { get; set; } = 0;
    public int RequestBodyCount { get; set; } = 0;
    public int ResponseCount { get; set; } = 0;
    public int OperationCount { get; set; } = 0;
    public int LinkCount { get; set; } = 0;
    public int CallbackCount { get; set; } = 0;

    public override void Visit(IOpenApiParameter parameter)
    {
        ParameterCount++;
    }

    public override void Visit(IOpenApiSchema schema)
    {
        SchemaCount++;
    }


    public override void Visit(IDictionary<string, IOpenApiHeader> headers)
    {
        HeaderCount++;
    }


    public override void Visit(IOpenApiPathItem pathItem)
    {
        PathItemCount++;
    }


    public override void Visit(IOpenApiRequestBody requestBody)
    {
        RequestBodyCount++;
    }


    public override void Visit(OpenApiResponses response)
    {
        ResponseCount++;
    }


    public override void Visit(OpenApiOperation operation)
    {
        OperationCount++;
    }


    public override void Visit(IOpenApiLink link)
    {
        LinkCount++;
    }

    public override void Visit(IOpenApiCallback callback)
    {
        CallbackCount++;
    }

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
