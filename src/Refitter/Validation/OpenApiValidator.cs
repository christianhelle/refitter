using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Validation;

public static class OpenApiValidator
{
    public static async Task<OpenApiValidationResult> Validate(string openApiFile)
    {
        var result = await OpenApiMultiFileReader.Read(openApiFile);

        var statsVisitor = new OpenApiStats();
        var walker = new OpenApiWalker(statsVisitor);
        walker.Walk(result.OpenApiDocument);

        return new(
            result.OpenApiDiagnostic,
            statsVisitor);
    }
}
