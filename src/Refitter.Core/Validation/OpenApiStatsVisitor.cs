using Microsoft.OpenApi;

namespace Refitter.Core.Validation;

/// <summary>
/// Counts the elements of a Microsoft.OpenApi document into an <see cref="OpenApiStats"/>.
/// </summary>
internal sealed class OpenApiStatsVisitor(OpenApiStats stats) : OpenApiVisitorBase
{
    public override void Visit(IOpenApiParameter parameter) => stats.ParameterCount++;

    public override void Visit(IOpenApiSchema schema) => stats.SchemaCount++;

    public override void Visit(IDictionary<string, IOpenApiHeader> headers) => stats.HeaderCount += headers.Count;

    public override void Visit(IOpenApiPathItem pathItem) => stats.PathItemCount++;

    public override void Visit(IOpenApiRequestBody requestBody) => stats.RequestBodyCount++;

    public override void Visit(OpenApiResponses response) => stats.ResponseCount += response.Count;

    public override void Visit(OpenApiOperation operation) => stats.OperationCount++;

    public override void Visit(IOpenApiLink link) => stats.LinkCount++;

    public override void Visit(IOpenApiCallback callback) => stats.CallbackCount++;
}
