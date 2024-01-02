using Microsoft.OpenApi.Services;

using Refitter.OAS;

namespace Refitter.Validation;

public static class OpenApiValidator
{
    public static async Task<OpenApiValidationResult> Validate(string openApiPath)
    {
        var result = await OpenApiReader.ParseOpenApi(openApiPath);

        var statsVisitor = new OpenApiStats();
        var walker = new OpenApiWalker(statsVisitor);
        walker.Walk(result.OpenApiDocument);

        return new(
            result.OpenApiDiagnostic,
            statsVisitor);
    }
}