using System.Threading;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

public static class OpenApiValidator
{
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        CancellationToken cancellationToken = default)
    {
        var result = await OpenApiMultiFileReader.Read(
            openApiFile,
            cancellationToken: cancellationToken);

        var statsVisitor = new OpenApiStats();
        var walker = new OpenApiWalker(statsVisitor);
        walker.Walk(result.OpenApiDocument);

        return new(
            result.OpenApiDiagnostic,
            statsVisitor);
    }
}
