using System.Net;
using System.Security;

using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

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