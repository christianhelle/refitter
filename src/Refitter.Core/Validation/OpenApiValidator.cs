using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Validates an OpenAPI specification file and collects statistics about its contents.
/// </summary>
public static class OpenApiValidator
{
    /// <summary>
    /// Validates an OpenAPI specification file and returns validation diagnostics and statistics.
    /// </summary>
    /// <param name="openApiFile">The path to the OpenAPI specification file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="OpenApiValidationResult"/> containing diagnostics and element counts.</returns>
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        CancellationToken cancellationToken = default)
    {
        await ReferenceGuard.ValidateAsync(openApiFile, allowRemoteReferences: false, cancellationToken)
            .ConfigureAwait(false);

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
