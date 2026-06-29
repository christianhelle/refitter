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
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="OpenApiValidationResult"/> containing diagnostics and element counts.</returns>
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        // For remote URLs, fetch once and validate before parsing
        if (PathUtilities.IsHttp(openApiFile))
        {
            string content;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                content = await client.GetStringAsync(openApiFile, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiFile}'.", ex);
            }

            await ReferenceGuard.ValidateAsync(openApiFile, content, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

            // Parse the already-fetched content
            var document = PathUtilities.IsYaml(openApiFile)
                ? await NSwag.OpenApiYamlDocument.FromYamlAsync(content, cancellationToken).ConfigureAwait(false)
                : await NSwag.OpenApiDocument.FromJsonAsync(content, cancellationToken).ConfigureAwait(false);

            var statsVisitor = new OpenApiStats();
            var walker = new OpenApiWalker(statsVisitor);
            walker.Walk(document);

            // Create a basic diagnostic since we're not using OpenApiMultiFileReader for remote URLs
            var diagnostic = new Microsoft.OpenApi.Readers.OpenApiDiagnostic();
            return new(diagnostic, statsVisitor);
        }

        // For local files, validate first (reads once), then parse with OpenApiMultiFileReader
        await ReferenceGuard.ValidateAsync(openApiFile, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);

        var result = await OpenApiMultiFileReader.Read(
            openApiFile,
            cancellationToken: cancellationToken);

        var stats = new OpenApiStats();
        var openApiWalker = new OpenApiWalker(stats);
        openApiWalker.Walk(result.OpenApiDocument);

        return new(
            result.OpenApiDiagnostic,
            stats);
    }
}
